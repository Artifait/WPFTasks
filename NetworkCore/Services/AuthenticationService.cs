
using TopNetwork.Services.MessageBuilder;
using System.Collections.Concurrent;
using TopNetwork.RequestResponse;
using TopNetwork.Core;

namespace TopNetwork.Services
{
    // Сервис для управления аутентификацией
    public class AuthenticationService<UserT> where UserT : User
    {
        private readonly UserService<UserT> _userService;
        private readonly MessageBuilderService _msgService;
        private readonly ConcurrentDictionary<TopClient, (string Login, DateTime Timestamp)> _authenticatedSessions = new();
        private readonly TimeSpan _maxSessionDuration;

        public LogString? Logger { get; set; }
        public int CountConnections { get =>  _authenticatedSessions.Count; }

        public AuthenticationService(UserService<UserT> userService, MessageBuilderService msgService, TimeSpan maxSessionDuration)
        {
            _msgService = msgService;
            _userService = userService;
            _maxSessionDuration = maxSessionDuration;
        }

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
            }
            else
            {
                return false;
            }

            await NotifySessionExpired(client);
            return false;
        }

        /// <summary>
        /// Обработка аутентификации.
        /// </summary>
        public async Task<Message?> AuthenticateClient(TopClient client, AuthenticationRequestData requestData)
        {
            var user = _userService.Authenticate(requestData.Login, requestData.Password);
            if (user != null)
            {
                if(!await user.IsUserLoginPossibleAsync())
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
            var clients = _authenticatedSessions.Keys.ToList();
            foreach (var client in clients)
            {
                await VerifySession(client);
            }
        }
    }
}
