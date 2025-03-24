
using System.Net;
using TopNetwork.Core;
using TopNetwork.RequestResponse;
using TopNetwork.Services;
using TopNetwork.Services.MessageBuilder;
using WPFTasks.Core.Models.Chat.MessageBuilder;
using WPFTasks.Core.Models.Chat.Services;
using WPFTasks.Core.Models.Core;

namespace WPFTasks.Core.Models.Chat
{
    public class ChatServer
    {
        private static readonly MessageBuilderService _msgService = new MessageBuilderService()
                    .Register(() => new AuthenticationResponseMessageBuilder())
                    .Register(() => new ChatMessageBuilder())
                    .Register(() => new ErroreMessageBuilder())
                    .Register(() => new EndSessionNotificationMessageBuilder())
                    .Register(() => new ServerOverloadedNotificationMessageBuilder());

        private readonly AuthenticationService<ChatUser> _authenticationService;
        private readonly Repository<ChatUser> _userRepository;
        private readonly UserService<ChatUser> _userService;
        private readonly TrackerUserActivityService _activityService;
        private readonly MessageCensorService _censorService;
        private readonly RrServerHandlerBase _handlers;
        private RrServer _server = new();

        // Словарь для хранения информации о банах: логин -> время окончания бана
        private readonly Dictionary<string, DateTime> _bannedUsers = new();

        public event Action<ChatUser, ChatMessageData>? OnReceivedMessageFromUser;
        public Logger Logger { get; private set; } = new();
        public EndPoint? EndPoint => _server.CurrentEndPoint;
        public bool IsRunning => _server.IsRunning;
        public int CountOpenSessions => _server.CountOpenSessions;

        public ChatServer(string? userFilePath = null)
        {
            _server.Logger = Logger.LogString;

            _userRepository = new(userFilePath ?? "ChatUsers.json");
            _userService = new(_userRepository, new PasswordService(), data => new(data.login, data.hashPassword));
            _authenticationService = new(_userService, _msgService) { Logger = Logger.LogString };
            _activityService = new(_msgService);
            _censorService = new MessageCensorService();

            _server
                .RegisterService(_msgService)
                .RegisterService(_userRepository)
                .RegisterService(_userService)
                .RegisterService(_authenticationService)
                .RegisterService(_activityService);

            _handlers = new RrServerHandlerBase()
                            .AddHandlerForMessageType(AuthenticationRequestData.MsgType, async (client, msg, context) =>
                            {
                                return await SafeWrapperForHandler(client, msg, context, async (client, msg, context) =>
                                {
                                    // Проверяем, не находится ли клиент в бане (если логин известен)
                                    var user = _authenticationService.GetUserBy(client);
                                    if (user != null && !string.IsNullOrEmpty(user.Login) && IsUserBanned(user.Login))
                                    {
                                        return _msgService.BuildMessage<ErroreMessageBuilder, ErroreData>(builder => builder
                                            .SetPayload("Вы заблокированы на сервере.")
                                        );
                                    }
                                    var requestData = AuthenticationRequestMessageBuilder.Parse(msg);
                                    return await _authenticationService.AuthenticateClient(client, requestData);
                                });
                            })
                            .AddHandlerForMessageType(RegisterRequestData.MsgType, async (client, msg, context) =>
                            {
                                return await SafeWrapperForHandler(client, msg, context, async (client, msg, context) =>
                                {
                                    var requestData = RegisterRequestMsgBuilder.Parse(msg);
                                    // Проверяем, не находится ли пользователь уже в бане (по логину)
                                    if (IsUserBanned(requestData.Login))
                                    {
                                        return _msgService.BuildMessage<ErroreMessageBuilder, ErroreData>(builder => builder
                                            .SetPayload("Регистрация невозможна. Пользователь заблокирован.")
                                        );
                                    }
                                    return await _authenticationService.RegisterClient(client, requestData);
                                });
                            })
                            .AddHandlerForMessageType(CloseSessionRequestData.MsgType, async (client, msg, context) =>
                            {
                                return await SafeWrapperForHandler(client, msg, context, async (client, msg, context) =>
                                {
                                    _authenticationService.CloseSession(client);
                                    return _msgService.BuildMessage<EndSessionNotificationMessageBuilder, EndSessionNotificationData>();
                                });
                            })
                            .AddHandlerForMessageType(ChatMessageData.MsgType, async (client, msg, context) =>
                            {
                                return await SafeWrapperForHandler(client, msg, context, async (client, msg, context) =>
                                {
                                    if (!_authenticationService.IsAuthClient(client))
                                    {
                                        return _msgService.BuildMessage<ErroreMessageBuilder, ErroreData>(builder => builder
                                            .SetPayload("Для использования данной функции нужно авторизироваться...")
                                        );
                                    }

                                    var msgData = ChatMessageBuilder.Parse(msg);
                                    bool badWords = _censorService.ProcessMessage(ref msgData);
                                    var user = _authenticationService.GetUserBy(client);

                                    if (badWords)
                                        Logger.LogString($"[ChatCensor]: пользователь под логином - [{user!.Login}], согрешил.");

                                    OnReceivedMessageFromUser?.Invoke(user!, msgData);

                                    return null;
                                });
                            });

            _server.SetSessionFactory(SessionFactory);
            UpdateAuthSessionDuration(Timeout.InfiniteTimeSpan).Wait();
        }

        public void SetEndPoint(IPEndPoint endPoint)
            => _server.SetEndPoint(endPoint);

        public async Task StartServer(CancellationToken token = default)
            => await _server.StartAsync(token);

        public async Task StopServer()
            => await _server.StopAsync();

        private async Task<ClientSession?> SessionFactory(TopClient client, ServiceRegistry context, LogString? logger)
        {
            // Если число активных сессий превышает лимит, отклоняем подключение
            if (_server.CountOpenSessions >= MaxConnections)
            {
                try
                {
                    client.SendMessageAsync(_msgService.BuildMessage<ServerOverloadedNotificationMessageBuilder, ServerOverloadedNotificationData>(null)).Wait();
                    logger?.Invoke($"[SessionFactory]: Отвергнуто подключение с [{client.RemoteEndPoint}], из-за перегрузки сервера...");
                    return null;
                }
                catch (Exception ex)
                {
                    logger?.Invoke($"[SessionFactory]: {ex.Message}.");
                    return null;
                }
            }

            // Можно добавить дополнительную проверку: если клиент пытается подключиться под забаненным логином, отклоняем
            ChatUser? user = _authenticationService.GetUserBy(client);
            if (user != null && !string.IsNullOrEmpty(user.Login) && IsUserBanned(user.Login))
            {
                try
                {
                    client.SendMessageAsync(_msgService.BuildMessage<ErroreMessageBuilder, ErroreData>(builder => builder
                        .SetPayload("Вы заблокированы на сервере.")
                    )).Wait();
                }
                catch { }
                return null;
            }

            ClientSession session = new(client, _handlers, context)
            {
                logger = logger,
            };

            session.OnMessageHandled += Session_OnMessageHandled;
            _activityService.UpdateLastActive(client);

            return await Task.FromResult(session);
        }

        private void Session_OnMessageHandled(ClientSession arg1, Message arg2)
        {
            try
            {
                _activityService.UpdateLastActive(arg1.Client);
                _server?.Logger?.Invoke($"[Server]: Обработано сообщение типа [{arg2.MessageType}] от [{arg1.RemoteEndPoint}] ");
            }
            catch { }
        }

        public void RegisterUser(string login, string password)
            => _userService.RegisterUser(login, password);

        public async Task<bool> SendMessageToUser(string login, string message)
        {
            var client = _authenticationService.GetTopClientBy(login);
            if (client != null)
            {
                var msg = _msgService.BuildMessage<ChatMessageBuilder, ChatMessageData>(b => b.SetPayload(message));
                await client.SendMessageAsync(msg);
            }
            return client != null;
        }

        public void DeleteUser(string login)
        {
            _userService.RemoveUser(login); 
        }

        public void BanUser(string login, TimeSpan duration)
        {
            _bannedUsers[login] = DateTime.UtcNow.Add(duration);
            var client = _authenticationService.GetTopClientBy(login);
            if (client != null)
            {
                _authenticationService.CloseSession(client);
            }
        }

        private bool IsUserBanned(string login)
        {
            if (_bannedUsers.TryGetValue(login, out DateTime banUntil))
            {
                if (DateTime.UtcNow < banUntil)
                    return true;
                else
                {
                    _bannedUsers.Remove(login);
                    return false;
                }
            }
            return false;
        }

        public string UserFilePath => _userRepository.FilePath;
        public TimeSpan MaxAuthSessionDuration => _authenticationService.MaxSessionDuration;
        public async Task UpdateAuthSessionDuration(TimeSpan newDuration)
            => await _authenticationService.UpdateSessionDuration(newDuration);
        public TimeSpan MaxDurationInactive => _activityService.MaxDurationInactive;
        public async Task UpdateMaxDurationInactive(TimeSpan newDuration)
            => await _activityService.UpdateMaxDurationInactive(newDuration);
        public int MaxConnections { get; set; } = 3;

        private async Task<Message> SafeWrapperForHandler(TopClient client, Message msg, ServiceRegistry context, Func<TopClient, Message, ServiceRegistry, Task<Message?>> handler)
        {
            try
            {
                return await handler?.Invoke(client, msg, context);
            }
            catch (Exception ex)
            {
                Logger.LogString($"[Server]: Ошибка обработки {msg.MessageType} от [{client.RemoteEndPoint}].\n{ex.Message}");
                return _msgService.BuildMessage<ErroreMessageBuilder, ErroreData>(builder => builder
                    .SetPayload($"Невозможно обработать {msg.MessageType}.\n{ex.Message}")
                );
            }
        }
    }
}
