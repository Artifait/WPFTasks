
using System.Net;
using TopNetwork.Conditions;
using TopNetwork.Core;
using TopNetwork.RequestResponse;
using TopNetwork.Services;
using TopNetwork.Services.MessageBuilder;
using WPFTasks.Core.Models.Currency.Conditions;
using WPFTasks.Core.Models.Currency.MessageBuilder;

namespace WPFTasks.Core.Models.Currency
{
    public class CurrencyServer
    { 
        // Регистрация всех фабрик для типов сообщений отправляемых сервером 
        private static readonly MessageBuilderService _msgService = new MessageBuilderService()
                    .Register(() => new AuthenticationResponseMessageBuilder())
                    .Register(() => new CurrencyResponseMessageBuilder())
                    .Register(() => new ErroreMessageBuilder())    
                    .Register(() => new EndSessionNotificationMessageBuilder())
                    .Register(() => new ServerOverloadedNotificationMessageBuilder());

        private readonly AuthenticationService<CurrencyUser> _authenticationService;
        private readonly Repository<CurrencyUser> _userRepository;
        private readonly UserService<CurrencyUser> _userService;
        private readonly RrServerHandlerBase _handlers;
        private readonly CurrencyConverter _converter;
        private RrServer _server = new();

        public Logger Logger { get; private set; } = new();
        public EndPoint? EndPoint => _server.CurrentEndPoint;
        public bool IsRunning => _server.IsRunning;
        public int CountOpenSessions => _server.CountOpenSessions;

        public CurrencyServer(string? filePath = null)
        {
            _converter = new(Logger.LogString);
            _server.Logger = Logger.LogString;

            _userRepository = new(filePath ?? "CurrencyUsers.json");
            _userService = new(_userRepository, new PasswordService(), data => new(data.login, data.hashPassword));
            _authenticationService = new(_userService, _msgService) { Logger = Logger.LogString };

            _server
                .RegisterService(_msgService)
                .RegisterService(_userRepository)
                .RegisterService(_userService)
                .RegisterService(_authenticationService);

            _handlers = new RrServerHandlerBase()
                .AddHandlerForMessageType(CurrencyRequestData.MsgType, async (client, msg, context) =>
                {
                    try
                    {
                        if(!_authenticationService.IsAuthClient(client))
                        {
                            return _msgService.BuildMessage<ErroreMessageBuilder, ErroreData>(builder => builder
                                .SetPayload("Для использования данной функции нужно авторизироваться...")
                            );
                        }
                        var user = _authenticationService.GetUserBy(client);
                        var requestData = CurrencyRequestMessageBuilder.Parse(msg);
                        var convertData = await _converter.GetExchangeRate(requestData.FromCurrency, requestData.ToCurrency);

                        var response = _msgService.BuildMessage<CurrencyResponseMessageBuilder, CurrencyResponseData>(builder => builder
                            .SetFromCurrency(requestData.FromCurrency)
                            .SetToCurrency(requestData.ToCurrency)
                            .SetRate(convertData)
                        );
                        user.AddCurrencyRequest();
                        _userService.UpdateUser(user);
                        return response;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogString($"[Server]: Ошибка обработки {CurrencyRequestData.MsgType} от [{client.RemoteEndPoint}].\n{ex.Message}");
                        return _msgService.BuildMessage<ErroreMessageBuilder, ErroreData>(builder => builder
                            .SetPayload($"Невозможно обработать {CurrencyRequestData.MsgType}.\n{ex.Message}")
                        );
                    }
                })
                .AddHandlerForMessageType(AuthenticationRequestData.MsgType, async (client, msg, context) =>
                {
                    try {
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
                try {
                    client.SendMessageAsync(_msgService.BuildMessage<ServerOverloadedNotificationMessageBuilder, ServerOverloadedNotificationData>(null)).Wait();
                    logger?.Invoke($"[SessionFactory]: Отвергнуто подключение с [{client.RemoteEndPoint}], из-за перегрузки сервера...");
                    return null;
                }
                catch (Exception ex) {
                    logger?.Invoke($"[SessionFactory]: {ex.Message}.");
                }
            }

            ClientSession session = new(client, _handlers, context)
            {
                logger = logger,
            };
            session.OnMessageProcessed += Session_OnMessageProcessed;

            return session;
        }

        private void Session_OnMessageProcessed(ClientSession arg1, Message arg2)
        {
            if(arg2.MessageType == CurrencyResponseData.MsgType)
            {
                if(!_authenticationService.GetUserBy(arg1.Client).IsUserLoginPossibleAsync().Result)
                {
                    arg1.SendMessage(_msgService.BuildMessage<EndSessionNotificationMessageBuilder, EndSessionNotificationData>(builder => builder
                        .SetPayload($"Вы сделали максимальное количество запросов...\nЧерез {CurrencyUser.Cooldown.TotalMinutes} минут вы снова сможете отправлять запросы."))).Wait();
                    _authenticationService.CloseSession(arg1.Client);
                    arg1.CloseSession();
                }
            }
        }

        public void RegisterUser(string login, string password)
            => _userService.RegisterUser(login, password);

        // Свойства Задаваемые юзером
        public string FilePath
        {
            get => _userRepository.FilePath;
            set => _userRepository.SetFilePath(value);
        }

        public TimeSpan MaxSessionDuration => _authenticationService.MaxSessionDuration;
        public async Task UpdateSessionDuration(TimeSpan newDuration)
            => await _authenticationService.UpdateSessionDuration(newDuration);

        public int MaxConnections { get; set; } = 3;
        public int MaxRequests
        {
            get => CurrencyUser.MaxRequests;
            set => CurrencyUser.MaxRequests = value;
        }

        public TimeSpan Cooldown
        {
            get => CurrencyUser.Cooldown;
            set => CurrencyUser.Cooldown = value;
        }

        public TimeSpan TimeWindow
        {
            get => CurrencyUser.TimeWindow;
            set => CurrencyUser.TimeWindow = value;
        }
    }
}
