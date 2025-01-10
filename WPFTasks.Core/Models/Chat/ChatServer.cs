
using System.Net;
using TopNetwork.Core;
using TopNetwork.RequestResponse;
using TopNetwork.Services;
using TopNetwork.Services.MessageBuilder;
using WPFTasks.Core.Models.Chat.MessageBuilder;
using WPFTasks.Core.Models.Core;

namespace WPFTasks.Core.Models.Chat
{
    public class ChatServer
    {
        // Регистрация всех фабрик для типов сообщений отправляемых сервером 
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
        private readonly RrServerHandlerBase _handlers;
        private RrServer _server = new();


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

            _server
                .RegisterService(_msgService)
                .RegisterService(_userRepository)
                .RegisterService(_userService)
                .RegisterService(_authenticationService)
                .RegisterService(_activityService);


            _handlers = new RrServerHandlerBase()
                            .AddHandlerForMessageType(AuthenticationRequestData.MsgType, async (client, msg, context) =>
                            {
                                try
                                {
                                    var requestData = AuthenticationRequestMessageBuilder.Parse(msg);
                                    return await _authenticationService.AuthenticateClient(client, requestData);
                                }
                                catch (Exception ex)
                                {
                                    Logger.LogString($"[Server]: Ошибка обработки {AuthenticationRequestData.MsgType} от [{client.RemoteEndPoint}].\n{ex.Message}");
                                    return _msgService.BuildMessage<ErroreMessageBuilder, ErroreData>(builder => builder
                                        .SetPayload($"Невозможно обработать {AuthenticationRequestData.MsgType}.\n{ex.Message}")
                                    );
                                }
                            })
                            .AddHandlerForMessageType(CloseSessionRequestData.MsgType, async (client, msg, context) =>
                            {
                                _authenticationService.CloseSession(client);
                                return _msgService.BuildMessage<EndSessionNotificationMessageBuilder, EndSessionNotificationData>();
                            })
                            .AddHandlerForMessageType(ChatMessageData.MsgType, async (client, msg, context) =>
                            {
                                try
                                {
                                    if (!_authenticationService.IsAuthClient(client))
                                    {
                                        return _msgService.BuildMessage<ErroreMessageBuilder, ErroreData>(builder => builder
                                            .SetPayload("Для использования данной функции нужно авторизироваться...")
                                        );
                                    }

                                    var requestData = ChatMessageBuilder.Parse(msg);
                                    var user = _authenticationService.GetUserBy(client);

                                    OnReceivedMessageFromUser?.Invoke(user!, requestData);

                                    return null;
                                }
                                catch (Exception ex)
                                {
                                    Logger.LogString($"[Server]: Ошибка обработки {ChatMessageData.MsgType} от [{client.RemoteEndPoint}].\n{ex.Message}");
                                    return _msgService.BuildMessage<ErroreMessageBuilder, ErroreData>(builder => builder
                                        .SetPayload($"Невозможно обработать {ChatMessageData.MsgType}.\n{ex.Message}")
                                    );
                                }
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

            ClientSession session = new(client, _handlers, context)
            {
                logger = logger,
            };

            session.OnMessageHandled += Session_OnMessageHandled;
            _activityService.UpdateLastActive(client);

            return session;
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

        // Свойства Задаваемые юзером
        public string UserFilePath => _userRepository.FilePath;

        public TimeSpan MaxAuthSessionDuration => _authenticationService.MaxSessionDuration;
        public async Task UpdateAuthSessionDuration(TimeSpan newDuration)
            => await _authenticationService.UpdateSessionDuration(newDuration);

        public TimeSpan MaxDurationInactive => _activityService.MaxDurationInactive;
        public async Task UpdateMaxDurationInactive(TimeSpan newDuration)
            => await _activityService.UpdateMaxDurationInactive(newDuration);

        public int MaxConnections { get; set; } = 3;
    }
}
