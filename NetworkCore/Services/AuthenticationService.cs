
using TopNetwork.Services.MessageBuilder;
using System.Collections.Concurrent;
using TopNetwork.RequestResponse;
using TopNetwork.Core;

namespace TopNetwork.Services
{
    public class AuthenticationService<UserT> where UserT : User
    {
        private readonly SemaphoreSlim _sessionsLock = new(1, 1);
        private readonly UserService<UserT> _userService;
        private readonly MessageBuilderService _msgService;
        private readonly ConcurrentDictionary<TopClient, ClientTimerSession<UserT>> _authenticatedSessions = new();
        private TimeSpan _maxSessionDuration = TimeSpan.FromMinutes(3);

        public LogString? Logger { get; set; }
        public TimeSpan MaxSessionDuration => _maxSessionDuration;
        public int CountAuthConnections => _authenticatedSessions.Count;

        public AuthenticationService(UserService<UserT> userService, MessageBuilderService msgService)
        {
            _msgService = msgService;
            _userService = userService;
        }

        public bool IsAuthClient(TopClient client) => _authenticatedSessions.ContainsKey(client);
        public UserT? GetUserBy(TopClient client) => _authenticatedSessions[client].User;
        public TopClient? GetTopClientBy(string login)
        {
            var res = _authenticatedSessions.Where(s => s.Value.Login == login);

            if(res.Any())
                return res.First().Key;

            return null;
        }
        public async Task<Message?> AuthenticateClient(TopClient client, AuthenticationRequestData requestData)
        {
            var user = _userService.Authenticate(requestData.Login, requestData.Password);
            if (user != null)
            {
                if (!await user.IsUserLoginPossibleAsync())
                {
                    Logger?.Invoke($"[AuthenticationService]: Клиент [{client.LastUseEndPoint}] не может использовать логин {requestData.Login}.");
                    return BuildFailedAuthResponse("Невозможно авторизоваться под этим логином.");
                }
                
                if (_authenticatedSessions.Values.Any(s => s.Login == requestData.Login))
                {
                    Logger?.Invoke($"[AuthenticationService]: Логин {requestData.Login} уже используется другим пользователем.");
                    return BuildFailedAuthResponse("Этот логин уже используется.");
                }

                var session = new ClientTimerSession<UserT>(client, user, _maxSessionDuration, NotifySessionExpired);
                client.OnConnectionLost += () => CloseSession(client);

                _authenticatedSessions[client] = session;
                Logger?.Invoke($"[AuthenticationService]: Клиент [{client.LastUseEndPoint}] успешно авторизован на {_maxSessionDuration.TotalMinutes} минут.");
                return BuildSuccessAuthResponse();
            }

            Logger?.Invoke($"[AuthenticationService]: Неверный логин или пароль от клиента [{client.LastUseEndPoint}].");
            return BuildFailedAuthResponse("Неверный логин или пароль.");
        }

        public async Task<Message?> RegisterClient(TopClient client, RegisterRequestData requestData)
        {
            if(_userService.GetUserByLogin(requestData.Login) != null)
            {
                Logger?.Invoke($"[AuthenticationService]: Клиент [{client.LastUseEndPoint}] пытается зарегаться под занятым логином...");
                return _msgService.BuildMessage<RegisterResponseMsgBuilder, RegisterResponseData>(builder => builder.SetExplanatoryMsg("Данный логин занят..."));
            }

            try
            {
                _userService.RegisterUser(requestData.Login, requestData.Password);
                Logger?.Invoke($"[AuthenticationService]: Клиент [{client.LastUseEndPoint}] успешно зарегал нового пользователя, под логином - {requestData.Login}.");
                return _msgService.BuildMessage<RegisterResponseMsgBuilder, RegisterResponseData>(builder => builder.SetExplanatoryMsg($"Вы успешно зарегистрировали нового пользователя, под логином - {requestData.Login}."));
            }
            catch (Exception ex)
            {
                Logger?.Invoke($"[AuthenticationService]: Ошибка при регистрации Клиента - [{client.LastUseEndPoint}]; Errore: {ex.Message}");
                return _msgService.BuildMessage<RegisterResponseMsgBuilder, RegisterResponseData>(builder => builder.SetExplanatoryMsg($"Ошибка регистрации: {ex.Message}"));
            }

        }
        public void CloseSession(TopClient client)
        {
            if (_authenticatedSessions.TryRemove(client, out var session))
            {
                session.Dispose();
                Logger?.Invoke($"[AuthenticationService]: Сессия клиента [{client.LastUseEndPoint}] закрыта.");
            }
        }

        private async Task NotifySessionExpired(TopClient client)
        {
            CloseSession(client);
            if(client.IsConnected)
            {
                try
                {
                    await client.SendMessageAsync(_msgService.BuildMessage<EndSessionNotificationMessageBuilder, EndSessionNotificationData>(builder => builder
                        .SetPayload("Ваша сессия истекла.")));
                    Logger?.Invoke($"[AuthenticationService]: Клиент [{client.RemoteEndPoint}] уведомлен об истечении сессии.");
                }
                catch (Exception ex)
                {
                    Logger?.Invoke($"[AuthenticationService]: Errore - {ex.Message}");
                }

            }

            client.Disconnect();
        }

        public async Task UpdateSessionDuration(TimeSpan newDuration)
        {
            await _sessionsLock.WaitAsync();
            try
            {
                _maxSessionDuration = newDuration;
                Logger?.Invoke($"[AuthenticationService]: Время длительности сессии обновлено до {_maxSessionDuration.TotalMinutes} минут.");

                foreach (var session in _authenticatedSessions.Values)
                {
                    session.UpdateDuration(newDuration);
                }
            }
            finally
            {
                _sessionsLock.Release();
            }
        }

        private Message BuildSuccessAuthResponse() =>
            _msgService.BuildMessage<AuthenticationResponseMessageBuilder, AuthenticationResponseData>(builder => builder
                .SetAuthentication(true)
                .SetExplanatoryMsg($"Вы успешно авторизовались!\nВаша сессия длится - " + (MaxSessionDuration == Timeout.InfiniteTimeSpan ? "ВЕЧНО" : $"{MaxSessionDuration.TotalMinutes} Мин.")));

        private Message BuildFailedAuthResponse(string reason) =>
            _msgService.BuildMessage<AuthenticationResponseMessageBuilder, AuthenticationResponseData>(builder => builder
                .SetAuthentication(false)
                .SetExplanatoryMsg(reason));
    }

    public class ClientTimerSession<UserT> where UserT : User
    {
        private readonly TopClient _client;
        private readonly Timer _timer;
        private readonly Func<TopClient, Task> _onSessionExpired;
        private readonly object _lock = new(); // Для потокобезопасности

        private DateTime _startTime; // Время последнего обновления
        private TimeSpan _remainingDuration; // Оставшееся время
        private bool _isDisposed; // Флаг для проверки состояния сессии

        public string Login => User.Login;
        public readonly UserT User;

        public ClientTimerSession(TopClient client, UserT user, TimeSpan duration, Func<TopClient, Task> onSessionExpired)
        {
            _client = client;
            User = user;

            _onSessionExpired = onSessionExpired;
            _remainingDuration = duration;
            _startTime = DateTime.UtcNow;

            _timer = new Timer(OnTimerElapsed, null, duration, Timeout.InfiniteTimeSpan);
        }

        public void UpdateDuration(TimeSpan newDuration)
        {
            lock (_lock)
            {
                if (_isDisposed)
                    return;

                // Вычисляем прошедшее время
                var elapsedTime = DateTime.UtcNow - _startTime;

                if (elapsedTime >= newDuration)
                {
                    // Таймер уже истёк или истечёт немедленно
                    TriggerExpiration();
                }
                else
                {
                    // Обновляем оставшееся время и перезапускаем таймер
                    _remainingDuration = newDuration - elapsedTime;
                    _startTime = DateTime.UtcNow;
                    _timer.Change(_remainingDuration, Timeout.InfiniteTimeSpan);
                }
            }
        }

        private void TriggerExpiration()
        {
            // Ручной вызов истечения таймера
            Dispose();
            _ = _onSessionExpired(_client);
        }

        private async void OnTimerElapsed(object? state)
        {
            lock (_lock)
            {
                if (_isDisposed)
                    return;
                _isDisposed = true; // Защищаем от повторного вызова
            }

            await _onSessionExpired(_client);
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_isDisposed)
                    return;

                _isDisposed = true;
                _timer.Dispose();
            }
        }
    }
}
