
using System.Net;
using TopNetwork.Core;
using TopNetwork.RequestResponse;
using TopNetwork.Services.MessageBuilder;
using TopNetwork.Services;
using WPFTasks.Core.Models.Core;
using WPFTasks.Core.Models.TicTacToe.MessageBuilder;
using WPFTasks.Core.Models.TicTacToe.Services;

namespace WPFTasks.Core.Models.TicTacToe
{
    public class TicTacToeServer
    {
        // Регистрация всех фабрик для типов сообщений отправляемых сервером 
        private static readonly MessageBuilderService _msgService = new MessageBuilderService()
                    .Register(() => new AuthenticationResponseMessageBuilder())
                    .Register(() => new ErroreMessageBuilder())
                    .Register(() => new EndSessionNotificationMessageBuilder())
                    .Register(() => new ServerOverloadedNotificationMessageBuilder())
                    .Register(() => new UpdateGameBoardMsgBuilder())
                    .Register(() => new GameEndedMsgBuilder())
                    .Register(() => new GameStartedMsgBuilder());

        private readonly AuthenticationService<TicTacToeUser> _authenticationService;
        private readonly Repository<TicTacToeUser> _userRepository;
        private readonly UserService<TicTacToeUser> _userService;
        private readonly TrackerUserActivityService _activityService;
        private readonly TicTacToeGameService _gameService;
        private readonly RrServerHandlerBase _handlers;
        private RrServer _server = new();

        public Logger Logger { get; private set; } = new();
        public EndPoint? EndPoint => _server.CurrentEndPoint;
        public bool IsRunning => _server.IsRunning;
        public int CountOpenSessions => _server.CountOpenSessions;


        public TicTacToeServer(string? userFilePath = null)
        {
            _server.Logger = Logger.LogString;

            _userRepository = new(userFilePath ?? "TicTacToeUsers.json");
            _userService = new(_userRepository, new PasswordService(), data => new(data.login, data.hashPassword));
            _authenticationService = new(_userService, _msgService) { Logger = Logger.LogString };
            _activityService = new(_msgService);
            _gameService = new();

            _server
                .RegisterService(_msgService)
                .RegisterService(_userRepository)
                .RegisterService(_userService)
                .RegisterService(_authenticationService)
                .RegisterService(_activityService)
                .RegisterService(_gameService);


            _handlers = new RrServerHandlerBase()
                            .AddHandlerForMessageType(AuthenticationRequestData.MsgType, async (client, msg, context) =>
                            {
                                return await SafeWrapperForHandler(client, msg, context, async (client, msg, context) =>
                                {
                                    var requestData = AuthenticationRequestMessageBuilder.Parse(msg);
                                    return await _authenticationService.AuthenticateClient(client, requestData);
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
                            .AddHandlerForMessageType(FindGameRequestData.MsgType, async (client, msg, context) =>
                            {
                                return await SafeWrapperForHandler(client, msg, context, async (client, msg, context) =>
                                {
                                    var requestData = FindGameRequestMsgBuilder.Parse(msg);
                                    return await _gameService.FindGameToClient(client, requestData);
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

        // Свойства Задаваемые юзером
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
