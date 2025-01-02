
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

        private static readonly SessionOpenConditionEvaluator _sessionOpenCondition = new SessionOpenConditionEvaluator()
            .AddAsyncCondition(new CurrencyOpenCondition());

        private static readonly SessionCloseConditionEvaluator _sessionCloseCondition = new SessionCloseConditionEvaluator()
            .AddAsyncCondition(new CurrencyCloseCondition());

        private readonly CurrencyConverter _converter;
        private readonly RrServerHandlerBase _handlers;
        private RrServer _server;    
        
        public Logger Logger { get; private set; }
        public AuthenticationService<CurrencyUser> AuthenticationService { get; private set; }

        public CurrencyServer()
        {
            Logger = new Logger();
            _converter = new(Logger.Log);
            
            _handlers = new RrServerHandlerBase()
                .AddHandlerForMessageType(CurrencyRequestData.MsgType, async (client, msg, context) =>
                {
                    try
                    {
                        var requestData = CurrencyRequestMessageBuilder.Parse(msg);
                        var convertData = await _converter.GetExchangeRate(requestData.FromCurrency, requestData.ToCurrency);

                        return _msgService.BuildMessage<CurrencyResponseMessageBuilder, CurrencyResponseData>(builder => builder
                            .SetFromCurrency(requestData.FromCurrency)
                            .SetToCurrency(requestData.ToCurrency)
                            .SetRate(convertData)
                        );
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"[Server]: Ошибка обработки {CurrencyRequestData.MsgType} от [{client.RemoteEndPoint}].\n{ex.Message}");
                        return _msgService.BuildMessage<ErroreMessageBuilder, ErroreData>(builder => builder
                            .SetPayload($"Невозможно обработать {CurrencyRequestData.MsgType}.\n{ex.Message}")
                        );
                    }
                })
                .AddHandlerForMessageType(AuthenticationRequestData.MsgType, async (client, msg, context) =>
                {
                    try {
                        var requestData = AuthenticationRequestMessageBuilder.Parse(msg);
                        return await AuthenticationService.AuthenticateClient(client, requestData); ;
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"[Server]: Ошибка обработки {AuthenticationRequestData.MsgType} от [{client.RemoteEndPoint}].\n{ex.Message}");
                        return _msgService.BuildMessage<ErroreMessageBuilder, ErroreData>(builder => builder
                            .SetPayload($"Невозможно обработать {AuthenticationRequestData.MsgType}.\n{ex.Message}")
                        );
                    }
                })
                .AddHandlerForMessageType(CloseSessionRequestData.MsgType, async (client, msg, context) =>
                {
                    AuthenticationService.CloseSession(client);
                    return _msgService.BuildMessage<EndSessionNotificationMessageBuilder, EndSessionNotificationData>();
                });
        }

        private ClientSession SessionFactory(TopClient client, ServiceRegistry context, LogString? logger)
        {
            ClientSession session = new(client, _handlers, context)
            {
                logger = logger,
                OpenConditionEvaluator = _sessionOpenCondition,
                CloseConditionEvaluator = _sessionCloseCondition,
            };

            return session;
        }
    }
}
