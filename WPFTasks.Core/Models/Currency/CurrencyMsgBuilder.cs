
using TopNetwork.Core;

namespace WPFTasks.Core.Models.Currency
{
    public static class CurrencyMsgBuilder
    {
        #region MessageTypes
        public static string GetMessageTypeStr(Types type) { return Enum.GetName(typeof(Types), type)!; }
        public enum Types
        {
            // Клиент
            AuthenticationRequest,
            CurrencyRateRequest,
            CloseSessionRequest,

            // Сервер
            Error,
            AuthenticationResult,
            CurrencyRateResult,
            /// <summary> Когда истекло время длительности сессии </summary>
            EndSessionNotification,
            ServerOverflowNotification,
        }
        #endregion
        #region Headers
        public static string GetMessageHeaderStr(Headers type) { return Enum.GetName(typeof(Headers), type)!; }
        public enum Headers
        {
            /// <summary> Аутентифицирован </summary>
            IsAuthed,
            /// <summary> Успешная операция </summary>
            IsSuccessfulOperation,
        }
        #endregion

        #region Authentication
        public static Message GetAuthenticationRequest(string login, string password)
        {
            return new Message
            {
                MessageType = GetMessageTypeStr(Types.AuthenticationRequest),
                Payload = $"{login} {password}"
            };
        }
        public static Message GetAuthenticationResult(bool isAuthenticated, string payload)
        {
            return new Message
            {
                MessageType = GetMessageTypeStr(Types.AuthenticationResult),
                Headers = { { GetMessageHeaderStr(Headers.IsAuthed), isAuthenticated.ToString() } },
                Payload = payload
            };
        }
        #endregion

        #region CurrencyRate
        public static Message GetCurrencyRateRequest(string fromCurrency, string toCurrency)
        {
            return new Message
            {
                MessageType = GetMessageTypeStr(Types.CurrencyRateRequest),
                Payload = $"{fromCurrency} {toCurrency}"
            };
        }
        public static Message GetCurrencyRateResult(string fromCurrency, string toCurrency, double? rate)
        {
            if(rate == null)
            {
                return new Message
                {
                    MessageType = GetMessageTypeStr(Types.CurrencyRateResult),
                    Headers = { { GetMessageHeaderStr(Headers.IsSuccessfulOperation), false.ToString() } },
                    Payload = $"Неподдерживаемое преобразование валют: {fromCurrency} to {toCurrency}"
                };
            }

            return new Message
            {
                MessageType = GetMessageTypeStr(Types.CurrencyRateResult),
                Headers = new Dictionary<string, string>
                {
                    { "FromCurrency", fromCurrency },
                    { "ToCurrency", toCurrency }
                },
                Payload = rate.ToString()!
            };
        }
        #endregion

        #region Notifications
        public static Message GetEndSessionNotification()
        {
            return new Message
            {
                MessageType = GetMessageTypeStr(Types.EndSessionNotification),
                Payload = "Время вашей сессии истекло, авторизируйтесь заного."
            };
        }
        public static Message GetServerOverflowNotification()
        {
            return new Message
            {
                MessageType = GetMessageTypeStr(Types.ServerOverflowNotification),
                Payload = "Cервер сейчас находится под максимальной нагрузкой и попробуйте подключиться через какое-то время."
            };
        }
        #endregion 

        public static Message GetCloseSessionRequest()
        {
            return new Message
            {
                MessageType = GetMessageTypeStr(Types.CloseSessionRequest),
            };
        }

        public static Message GetErroreMsg(Dictionary<string, string> headers, string payload)
        {
            return new Message
            {
                MessageType = GetMessageTypeStr(Types.Error),
                Headers = headers,
                Payload = payload
            };
        }
    }
}
