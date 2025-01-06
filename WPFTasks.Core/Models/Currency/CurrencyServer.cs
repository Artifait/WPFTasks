
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

        private readonly ConnectionLimitCondition _openCondition = new();
        private readonly SessionOpenConditionEvaluator _sessionOpenCondition = new();

        private readonly MaxRequestsCloseCondition _closeCondition = new() { MsgType = CurrencyRequestData.MsgType };
        private readonly SessionCloseConditionEvaluator _sessionCloseCondition = new SessionCloseConditionEvaluator()
            .AddAsyncCondition(new AuthCloseCondition());

        private readonly AuthenticationService<CurrencyUser> _authenticationService;
        private readonly Repository<CurrencyUser> _userRepository;
        private readonly UserService<CurrencyUser> _userService;
        private readonly RrServerHandlerBase _handlers;
        private readonly CurrencyConverter _converter;
        private RrServer _server = new();

        public Logger Logger { get; private set; } = new();
        public EndPoint? EndPoint => _server.CurrentEndPoint;

        public CurrencyServer(string? filePath = null)
        {
            _converter = new(Logger.LogString);
            _server.Logger = Logger.LogString;

            _userRepository = new(filePath ?? "CurrencyUsers.json");

            _userService = new(_userRepository, new PasswordService(), data => new(data.login, data.hashPassword));
            //_userService
            //    .RegisterUser("Art", "123")
            //    .RegisterUser("User1", "123")
            //    .RegisterUser("Peshka", "123");

            _server
                .RegisterGeneric(typeof(AuthenticationService<>), typeof(AuthenticationService<>))
                .RegisterService(_msgService)
                .RegisterService(_userRepository)
                .RegisterService(_userService)
                .Context.TryGetService(out _authenticationService!);

            _sessionOpenCondition.AddAsyncCondition(_openCondition);
            _sessionCloseCondition.AddCondition(_closeCondition);

            _handlers = new RrServerHandlerBase()
                .AddHandlerForMessageType(CurrencyRequestData.MsgType, async (client, msg, context) =>
                {
                    try
                    {
                        if(!_authenticationService.IsAuthClient(client))
                        {
                            return _msgService.BuildMessage<ErroreMessageBuilder, ErroreData>(builder => builder
                                .SetPayload("Для использования данной функции нужно быть авторизироваться...")
                            );
                        }
                        var requestData = CurrencyRequestMessageBuilder.Parse(msg);
                        var convertData = await _converter.GetExchangeRate(requestData.FromCurrency, requestData.ToCurrency);

                        var response = _msgService.BuildMessage<CurrencyResponseMessageBuilder, CurrencyResponseData>(builder => builder
                            .SetFromCurrency(requestData.FromCurrency)
                            .SetToCurrency(requestData.ToCurrency)
                            .SetRate(convertData)
                        );

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

        private ClientSession? SessionFactory(TopClient client, ServiceRegistry context, LogString? logger)
        {
            ClientSession session = new(client, _handlers, context)
            {
                logger = logger,
                OpenConditionEvaluator = _sessionOpenCondition,
                CloseConditionEvaluator = _sessionCloseCondition,
            };

            return session;
        }

        // Свойства Задаваемые юзером
        public string FilePath
        {
            get => _userRepository.FilePath;
            set => _userRepository.SetFilePath(value);
        }

        public TimeSpan MaxSessionDuration => _authenticationService.MaxSessionDuration;
        public async Task UpdateSessionDuration(TimeSpan newDuration)
            => await _authenticationService.UpdateSessionDuration(newDuration);

        public int MaxConnections
        {
            set => _openCondition.MaxConnections = value;
            get => _openCondition.MaxConnections;
        }

        public int MaxRequests
        {
            get => _closeCondition.MaxRequests;
            set => _closeCondition.MaxRequests = value;
        }
    }
}
