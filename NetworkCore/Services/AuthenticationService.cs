
using TopNetwork.Services.MessageBuilder;
using System.Collections.Concurrent;
using TopNetwork.RequestResponse;
using TopNetwork.Core;

namespace TopNetwork.Services
{
    public class AuthenticationService<UserT> where UserT : User
    {
        private readonly SemaphoreSlim _verifyAllSessionsSemaphore = new(1, 1); // Для синхронизации вызова VerifyAllSessions & UpdateSessionDuration
        private readonly UserService<UserT> _userService;
        private readonly MessageBuilderService _msgService;
        private readonly ConcurrentDictionary<TopClient, (string Login, DateTime Timestamp)> _authenticatedSessions = new();
        private TimeSpan _maxSessionDuration = TimeSpan.FromMinutes(10);

        public LogString? Logger { get; set; }
        public TimeSpan MaxSessionDuration => _maxSessionDuration;
        public int CountConnections => _authenticatedSessions.Count;

        public AuthenticationService(UserService<UserT> userService, MessageBuilderService msgService)
        {
            _msgService = msgService;
            _userService = userService;
        }

        public bool IsAuthClient(TopClient client) => _authenticatedSessions.TryGetValue(client, out _);

        /// <summary>
        /// Проверка текущей сессии.
        /// </summary>
        public async Task<bool> VerifySession(TopClient client)
        {
            if (_authenticatedSessions.TryGetValue(client, out var session))
            {
                if (DateTime.UtcNow - session.Timestamp < _maxSessionDuration)
                    return true;

                Logger?.Invoke($"[AuthenticationService]: Сессия клиента [{client.RemoteEndPoint}] истекла.");
                _authenticatedSessions.Remove(client, out var _);
                await NotifySessionExpired(client);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Обработка аутентификации.
        /// </summary>
        public async Task<Message?> AuthenticateClient(TopClient client, AuthenticationRequestData requestData)
        {
            var user = _userService.Authenticate(requestData.Login, requestData.Password);
            if (user != null)
            {
                if (!await user.IsUserLoginPossibleAsync())
                {
                    Logger?.Invoke($"[AuthenticationService]: Клиент [{client.RemoteEndPoint}] пытается авторизоваться под логином {requestData.Login}, который в данный момент не подлежит авторизации.");
                    return _msgService.BuildMessage<AuthenticationResponseMessageBuilder, AuthenticationResponseData>(builder => builder
                        .SetAuthentication(false)
                        .SetExplanatoryMsg("Невозможно авторизоваться под этим логином...")
                    );
                }

                _authenticatedSessions[client] = (requestData.Login, DateTime.UtcNow);

                Logger?.Invoke($"[AuthenticationService]: Клиент [{client.RemoteEndPoint}] успешно аутентифицирован под логином {requestData.Login}.");
                return _msgService.BuildMessage<AuthenticationResponseMessageBuilder, AuthenticationResponseData>(builder => builder
                    .SetAuthentication(true)
                    .SetExplanatoryMsg("Вы успешно авторизовались!")
                );
            }

            Logger?.Invoke($"[AuthenticationService]: Клиент [{client.RemoteEndPoint}] ввёл неверный логин или пароль.");
            return _msgService.BuildMessage<AuthenticationResponseMessageBuilder, AuthenticationResponseData>(builder => builder
                .SetAuthentication(false)
                .SetExplanatoryMsg("Неверный Логин или Пароль.")
            );
        }

        /// <summary>
        /// Закрытие сессии клиента.
        /// </summary>
        public void CloseSession(TopClient client)
        {
            _authenticatedSessions.Remove(client, out var _);
            Logger?.Invoke($"[AuthenticationService]: Сессия клиента [{client.RemoteEndPoint}] была закрыта.");
        }

        /// <summary>
        /// Уведомление клиента об истечении сессии.
        /// </summary>
        private async Task NotifySessionExpired(TopClient client)
        {
            await client.SendMessageAsync(_msgService.BuildMessage<EndSessionNotificationMessageBuilder, EndSessionNotificationData>(builder => builder
                .SetPayload("Ваша сессия истекла...")
            ));

            Logger?.Invoke($"[AuthenticationService]: Клиент [{client.RemoteEndPoint}] был уведомлен об истечении сессии.");
            client.Disconnect();
        }

        /// <summary>
        /// Проверка всех активных сессий.
        /// </summary>
        public async Task VerifyAllSessions()
        {
            try
            {
                await _verifyAllSessionsSemaphore.WaitAsync(); // Ожидаем, пока не завершится текущая проверка сессий

                var clients = _authenticatedSessions.Keys.ToList();
                foreach (var client in clients)
                {
                    await VerifySession(client);
                }
            }
            finally
            {
                _verifyAllSessionsSemaphore.Release(); // Освобождаем семафор
            }
        }

        /// <summary>
        /// Динамическое изменение времени длительности сессии и вызов метода VerifyAllSessions.
        /// </summary>
        public async Task UpdateSessionDuration(TimeSpan newDuration)
        {
            // Ожидаем, если кто-то уже работает с VerifyAllSessions
            await _verifyAllSessionsSemaphore.WaitAsync();

            try
            {
                _maxSessionDuration = newDuration;
                Logger?.Invoke($"[AuthenticationService]: Время длительности сессии было изменено на {_maxSessionDuration.TotalMinutes} минут.");


                await VerifyAllSessions();
            }
            finally
            {
                _verifyAllSessionsSemaphore.Release();
            }
        }
    }
}
