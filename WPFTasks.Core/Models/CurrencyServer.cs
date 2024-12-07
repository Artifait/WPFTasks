
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using TopNetwork.Core;

namespace WPFTasks.Core.Models
{
    public class CurrencyServer
    {
        public readonly int MaxCoutActiveConnection = 10;
        public int CountActiveConnection { get; private set; }
        public RequestResponseServer Server { get; set; }
        public static string GetMessageTypeStr(MessageType type) { return Enum.GetName(typeof(MessageType), type); }
        public enum MessageType
        {
            CurrencyConversion,
            CurrencyRate,
            Authentication,
            Error
        }
        public static string GetCurrencyTypeStr(CurrencyType type) { return Enum.GetName(typeof(CurrencyType), type); }
        public enum CurrencyType
        {
            USD,
            RUB,
            EUR
        }

        public StringBuilder Logger { get; set; } = new();
        public UserManager UserManager { get; set; }
        public Action<string> OnUpdateLog { get; set; }
        public Dictionary<TopClient, bool> AuthenticatedConnection { get; private set; } = [];

        public void LogLine(string str)
        {
            Logger.AppendLine(str);
            OnUpdateLog?.Invoke(Logger.ToString());
        }

        public async Task<double?> GetExchangeRate(CurrencyType fromCurrency, CurrencyType toCurrency)
            => await GetExchangeRate(GetCurrencyTypeStr(fromCurrency), GetCurrencyTypeStr(toCurrency));
        public async Task<double?> GetExchangeRate(string fromCurrency, string toCurrency)
        {
            using var httpClient = new HttpClient();

            try
            {
                string apiUrl = "https://api.exchangerate-api.com/v4/latest/" + fromCurrency;  // Бесплатный API
                string response = await httpClient.GetStringAsync(apiUrl);
                JObject data = JObject.Parse(response);
                return data["rates"]?[toCurrency]?.ToObject<double>();
            }
            catch (Exception ex)
            {
                LogLine($"Ошибка при получении курса: {ex.Message}");
                return null;
            }
        }

        public CurrencyServer(IPAddress ip, int port) 
        {
            UserManager = new UserManager("user_credentials.json") { Logger = LogLine };
            UserManager.LoadCredentials();
            
            Server = new RequestResponseServer
            {
                Logger = LogLine,
                ShouldAcceptClient = async client =>
                {
                    LogLine("Checking if client should be accepted...");
                    return client != null && CountActiveConnection < MaxCoutActiveConnection;
                }
            };
            Server.ClientConnected += client =>
            {
                CountActiveConnection++;
                LogLine($"Client connected: {client.RemoteEndPoint}");
            };

            Server.ClientDisconnected += client =>
            {
                CountActiveConnection--;
                LogLine($"Client disconnected: {client.RemoteEndPoint}");
            };

            Server.ClientRejected += async client =>
            {
                LogLine($"Client rejected: {client.RemoteEndPoint}");
            };

            Server.Init(ip, port);

            Server.ServerHandlers.AddHandlerForMessageType(GetMessageTypeStr(MessageType.Authentication), async (client, message) =>
                await UserManager.HandleAuthentication(client, message, AuthenticatedConnection));

            Server.ServerHandlers.AddHandlerForMessageType(GetMessageTypeStr(MessageType.CurrencyConversion), CurrencyConversionHandler);

            Server.ServerHandlers.SetDefaultHandler(async (client, message) => new Message { 
                MessageType = GetMessageTypeStr(MessageType.Error),
                Payload = "Мы не смогли обработать ваш запрос..." 
            });
        }

        #region MainHandlers

        private async Task<Message?> CurrencyConversionHandler(TopClient client, Message message)
        {
            if (!AuthenticatedConnection.TryGetValue(client, out var isAuthenticated) || !isAuthenticated)
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

            double? exchangeRate = await GetExchangeRate(fromCurrency, toCurrency);

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
