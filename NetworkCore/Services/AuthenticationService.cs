
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
        public UserT GetUserBy(TopClient client) => _authenticatedSessions[client].User;

        public async Task<Message?> AuthenticateClient(TopClient client, AuthenticationRequestData requestData)
        {
            var user = _userService.Authenticate(requestData.Login, requestData.Password);
            if (user != null)
            {
                if (!await user.IsUserLoginPossibleAsync())
                {
                    Logger?.Invoke($"[AuthenticationService]: Клиент [{client.RemoteEndPoint}] не может использовать логин {requestData.Login}.");
                    return BuildFailedAuthResponse("Невозможно авторизоваться под этим логином.");
                }
                
                if (_authenticatedSessions.Values.Any(s => s.Login == requestData.Login))
                {
                    Logger?.Invoke($"[AuthenticationService]: Логин {requestData.Login} уже используется другим пользователем.");
                    return BuildFailedAuthResponse("Этот логин уже используется.");
                }

                var session = new ClientTimerSession<UserT>(client, user, _maxSessionDuration, NotifySessionExpired);
                _authenticatedSessions[client] = session;
                Logger?.Invoke($"[AuthenticationService]: Клиент [{client.RemoteEndPoint}] успешно авторизован на {_maxSessionDuration.TotalMinutes} минут.");
                return BuildSuccessAuthResponse();
            }

            Logger?.Invoke($"[AuthenticationService]: Неверный логин или пароль от клиента [{client.RemoteEndPoint}].");
            return BuildFailedAuthResponse("Неверный логин или пароль.");
        }

        public void CloseSession(TopClient client)
        {
            if (_authenticatedSessions.TryRemove(client, out var session))
            {
                session.Dispose();
                Logger?.Invoke($"[AuthenticationService]: Сессия клиента [{client.RemoteEndPoint}] закрыта.");
            }
        }

        private async Task NotifySessionExpired(TopClient client)
        {
            CloseSession(client);
            await client.SendMessageAsync(_msgService.BuildMessage<EndSessionNotificationMessageBuilder, EndSessionNotificationData>(builder => builder
                .SetPayload("Ваша сессия истекла.")));
            Logger?.Invoke($"[AuthenticationService]: Клиент [{client.RemoteEndPoint}] уведомлен об истечении сессии.");
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
                .SetExplanatoryMsg($"Вы успешно авторизовались!\nВаша сессия длится - {MaxSessionDuration.TotalMinutes} Мин."));

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

        public string Login => User.Login;
        public readonly UserT User;
        public ClientTimerSession(TopClient client, UserT user, TimeSpan duration, Func<TopClient, Task> onSessionExpired)
        {
            _client = client;
            User = user;

            _onSessionExpired = onSessionExpired;
            _timer = new Timer(OnTimerElapsed, null, duration, Timeout.InfiniteTimeSpan);
        }

        public void UpdateDuration(TimeSpan newDuration)
        {
            _timer.Change(newDuration, Timeout.InfiniteTimeSpan);
        }

        private async void OnTimerElapsed(object? state)
        {
            await _onSessionExpired(_client);
        }

        public void Dispose()
        {
            _timer.Dispose();
        }
    }
}
