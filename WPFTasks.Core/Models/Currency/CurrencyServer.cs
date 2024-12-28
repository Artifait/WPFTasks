
using TopNetwork.RequestResponse;
using TopNetwork.Services.MessageBuilder;
using WPFTasks.Core.Models.Currency.MessageBuilder;

namespace WPFTasks.Core.Models.Currency
{
    public class CurrencyServer
    {
        // Регистрация всех фабрик для типов сообщений отправляемых сервером 
        private static readonly MessageBuilderService _msgService = new MessageBuilderService()
                    .Register(() => new CurrencyResponseMessageBuilder())
                    .Register(() => new AuthenticationResponseMessageBuilder())
                    .Register(() => new EndSessionNotificationMessageBuilder());

        private static readonly RrServerHandlerBase _handlers = new RrServerHandlerBase()
            .AddHandlerForMessageType(CurrencyRequestData.MsgType, async (client, msg, context) =>
            {
                try {
                    var data = CurrencyRequestMessageBuilder.Parse(msg);
                }
                catch {
                    Log
                }
                return _msgService.BuildMessage<CurrencyResponseMessageBuilder, CurrencyResponseData>(builder => builder.);
            });

        private static readonly CurrencyConverter Converter = new();

        public Logger Logger { get; private set; }
        public UserManager UserManager { get; set; }

        public CurrencyServer()
        {
            Logger = new();
        }
    }
}
