
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
        public static string GetHeaderStr(Headers type) { return Enum.GetName(typeof(Headers), type)!; }
        public enum Headers
        {
            /// <summary> Аутентифицирован </summary>
            IsAuthed,
            /// <summary> Успешная операция </summary>
            IsSuccessfulOperation,
            FromCurrency,
            ToCurrency
        }
        #endregion

        #region Authentication
        public static Message CreateAuthenticationRequest(string login, string password)
        {
            return new Message
            {
                MessageType = GetMessageTypeStr(Types.AuthenticationRequest),
                Payload = $"{login} {password}"
            };
        }
        public static Message CreateAuthenticationResult(bool isAuthenticated, string payload)
        {
            return new Message
            {
                MessageType = GetMessageTypeStr(Types.AuthenticationResult),
                Headers = { { GetHeaderStr(Headers.IsAuthed), isAuthenticated.ToString() } },
                Payload = payload
            };
        }
        #endregion

        #region CurrencyRate
        public static Message CreateCurrencyRateRequest(string fromCurrency, string toCurrency)
        {
            return new Message
            {
                MessageType = GetMessageTypeStr(Types.CurrencyRateRequest),
                Headers = new Dictionary<string, string>
                {
                    { GetHeaderStr(Headers.FromCurrency), fromCurrency },
                    { GetHeaderStr(Headers.ToCurrency), toCurrency }
                },
            };
        }
        public static Message CreateCurrencyRateResult(string fromCurrency, string toCurrency, double? rate)
        {
            if(rate == null)
            {
                return new Message
                {
                    MessageType = GetMessageTypeStr(Types.CurrencyRateResult),
                    Headers = 
                    { 
                        { GetHeaderStr(Headers.IsSuccessfulOperation), false.ToString() }
                    },
                    Payload = $"Неподдерживаемое преобразование валют: {fromCurrency} в {toCurrency}"
                };
            }

            return new Message
            {
                MessageType = GetMessageTypeStr(Types.CurrencyRateResult),
                Headers =
                {
                    { GetHeaderStr(Headers.IsSuccessfulOperation), true.ToString() },
                    { GetHeaderStr(Headers.FromCurrency), fromCurrency },
                    { GetHeaderStr(Headers.ToCurrency), toCurrency },
                },
                Payload = rate.ToString()!
            };
        }
        #endregion

        #region Notifications
        public static Message CreateEndSessionNotification()
        {
            return new Message
            {
                MessageType = GetMessageTypeStr(Types.EndSessionNotification),
                Payload = "Время вашей сессии истекло, авторизируйтесь заного."
            };
        }
        public static Message CreateServerOverflowNotification()
        {
            return new Message
            {
                MessageType = GetMessageTypeStr(Types.ServerOverflowNotification),
                Payload = "Cервер сейчас находится под максимальной нагрузкой и попробуйте подключиться через какое-то время."
            };
        }
        #endregion 

        public static Message CreateCloseSessionRequest()
        {
            return new Message
            {
                MessageType = GetMessageTypeStr(Types.CloseSessionRequest),
            };
        }

        public static Message CreateErroreMsg(Dictionary<string, string>? headers = null, string? payload = null)
        {
            return new Message
            {
                MessageType = GetMessageTypeStr(Types.Error),
                Headers = headers ?? [],
                Payload = payload ?? string.Empty
            };
        }
    }
}
