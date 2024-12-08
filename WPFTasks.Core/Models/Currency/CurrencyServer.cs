
using System.Net;
using TopNetwork.Core;

namespace WPFTasks.Core.Models.Currency
{
    public class CurrencyServer
    {
        public RequestResponseServer Server { get; set; }

        public CurrencyStatus Status
        {
            get => (CurrencyStatus)Server.Status;
        }
        public Logger Logger { get; private set; } = new();
        public UserManager UserManager { get; set; }
        public CurrencyConverter Converter { get; private set; } = new();

        public CurrencyServer(IPAddress ip, int port)
        {
            UserManager = new UserManager("user_credentials.json") { Logger = Logger.Log };
            UserManager.LoadCredentials();

            Server = new RequestResponseServer
            {
                Logger = Logger.Log,
                ShouldAcceptClient = async client =>
                {
                    Logger.Log("Checking if client should be accepted...");
                    return client != null && Status.ActiveConnections < Status.MaxActiveConnection;
                }
            };

            Server.ClientConnected += client => Status.ActiveConnections++;
            Server.ClientDisconnected += client => Status.ActiveConnections--;

            Server.Init(ip, port);

            Server.ServerHandlers.AddHandlerForMessageType(GetMessageTypeStr(MessageType.Authentication), async (client, message) =>
                await UserManager.HandleAuthentication(client, message));

            Server.ServerHandlers.AddHandlerForMessageType(GetMessageTypeStr(MessageType.CloseConnection), async (client, message) =>
                await UserManager.HandleDisconnection(client, message));

            Server.ServerHandlers.AddHandlerForMessageType(GetMessageTypeStr(MessageType.CurrencyConversion), CurrencyConversionHandler);

            Server.ServerHandlers.SetDefaultHandler(async (client, message) => new Message
            {
                MessageType = GetMessageTypeStr(MessageType.Error),
                Payload = "Мы не смогли обработать ваш запрос..."
            });
        }

        #region MainHandler

        private async Task<Message?> CurrencyConversionHandler(TopClient client, Message message)
        {
            if (!UserManager.CheckAuthenticatedConnection(client, message.Headers["Login"]))
            {
                return new Message
                {
                    MessageType = GetMessageTypeStr(MessageType.Error),
                    Payload = "Пройдите аутентификацию, перед началом использования."
                };
            }

            string[] currencies = message.Payload.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (currencies.Length != 2)
            {
                return new Message
                {
                    MessageType = "Error",
                    Payload = "Неверный формат Payload нагрузки. Ожидаемый формат: '<FROM_CURRENCY> <TO_CURRENCY>'"
                };
            }

            string fromCurrency = currencies[0];
            string toCurrency = currencies[1];

            double? exchangeRate = await Converter.GetExchangeRate(fromCurrency, toCurrency);

            if (exchangeRate == null)
            {
                return new Message
                {
                    MessageType = "Error",
                    Payload = $"Неподдерживаемое преобразование валют: {fromCurrency} to {toCurrency}"
                };
            }

            return new Message
            {
                MessageType = GetMessageTypeStr(MessageType.CurrencyRate),
                Headers = new Dictionary<string, string>
                {
                    { "FromCurrency", fromCurrency },
                    { "ToCurrency", toCurrency }
                },
                Payload = exchangeRate.ToString()!
            };
        }
        #endregion
    }
}
