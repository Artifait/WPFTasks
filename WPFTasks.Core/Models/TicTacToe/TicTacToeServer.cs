
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
                    .Register(() => new ErroreMessageBuilder())
                    .Register(() => new EndSessionNotificationMessageBuilder())
                    .Register(() => new ServerOverloadedNotificationMessageBuilder());

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

            _activityService = new(_msgService);
            _gameService = new() { Logger = Logger.LogString };

            _server
                .RegisterService(_msgService)
                .RegisterService(_activityService)
                .RegisterService(_gameService);


            _handlers = new RrServerHandlerBase()
                .AddHandlerForMessageType(FindGameRequestData.MsgType, async (client, msg, context) =>
                {
                    return await SafeWrapperForHandler(client, msg, context, async (client, msg, context) =>
                    {
                        var requestData = FindGameRequestMsgBuilder.Parse(msg);
                        await _gameService.FindGameToClient(client, requestData);
                        return null;
                    });
                });

            _server.SetSessionFactory(SessionFactory);
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

        // Свойства Задаваемые юзером
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
