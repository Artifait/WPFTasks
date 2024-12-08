
using TopNetwork.Core;

namespace WPFTasks.Core.Models.Currency
{
    using MsgT = CurrencyServer.MessageType;

    public class CurrencyClient
    {
        public bool _authenticated = false;
        public bool Authenticated
        {
            get => _authenticated;
            set
            {
                _authenticated = value;
                OnAuthenticated?.Invoke(value);
            }
        }

        private TopClient? _client;
        private CancellationTokenSource? _cts;
        /// <summary>
        /// 1) string - FromCurrency
        /// 2) string - ToCurrency
        /// 3) double - rate
        /// </summary>
        public event Action<string, string, double>? OnGetCurrencyRateMsg;
        public event Action<bool>? OnAuthenticated;
        public event Action<string>? OnGetErrorMsg;

        public CurrencyClient(string serverIp, int port, string login, string password)
        {
            _ = Init(serverIp, port, login, password);
        }
        public CurrencyClient() { }

        public async Task Init(string serverIp, int port, string login, string password)
        {
            await TryDisconnect();

            _client = new TopClient(serverIp, port);
            _cts = new CancellationTokenSource();

            _client.OnAcceptedMessage += OnMessageFromServer;
            _client.OnDisconnected += () => Authenticated = false;

            _ = _client.StartListen(_cts.Token);

            _ = Authentication(login, password);
        }
        public async Task Authentication(string login, string password)
        {
            if (_client == null) throw new NullReferenceException("Не инициализированный клиент.");

            Message request = new()
            {
                MessageType = CurrencyServer.GetMessageTypeStr(MsgT.Authentication),
                Payload = $"{login} {password}"
            };

            await _client.SendMessageAsync(request);
        }
        public async Task RequestExchangeRate(string fromCurrency, string toCurrency)
        {
            if (_client == null) throw new NullReferenceException("Не инициализированный клиент.");

            Message request = new()
            {
                MessageType = CurrencyServer.GetMessageTypeStr(MsgT.CurrencyConversion),
                Payload = $"{fromCurrency} {toCurrency}"
            };

            await _client.SendMessageAsync(request);
        }

        private void OnMessageFromServer(Message msg)
        {
            if (msg.MessageType == CurrencyServer.GetMessageTypeStr(MsgT.Authentication))
            {
                if (msg.Headers.TryGetValue("Authenticated", out string value))
                {
                    if (bool.TryParse(value, out bool res))
                    {
                        Authenticated = res;
                    }
                    else
                    {
                        OnGetErrorMsg?.Invoke($"Почему то не смогли распарсить в bool строку: {value}.");
                    }
                }
                else
                {
                    OnGetErrorMsg?.Invoke($"Почему то не смогли распарсить в bool строку: {value}.");
                }
            }
            else if (msg.MessageType == CurrencyServer.GetMessageTypeStr(MsgT.CurrencyRate))
            {
                try
                {
                    OnGetCurrencyRateMsg?.Invoke(msg.Headers["FromCurrency"], msg.Headers["ToCurrency"], double.Parse(msg.Payload));
                }
                catch (Exception ex) { OnGetErrorMsg?.Invoke($"Не смогли распарсить ответ от сервера.\nОшибка: {ex.Message}."); }
            }
            else if (msg.MessageType == CurrencyServer.GetMessageTypeStr(MsgT.Error))
            {
                OnGetErrorMsg?.Invoke($"Ошибка: {msg.Payload}.");
            }
            else
            {
                OnGetErrorMsg?.Invoke("Неизвестный тип сообщения.");
            }
        }
        public async Task TryDisconnect()
        {
            if(_client?.IsConnected ?? false)
            {
                Message request = new()
                {
                    MessageType = CurrencyServer.GetMessageTypeStr(MsgT.CloseConnection),
                };

                await _client.SendMessageAsync(request);

                _cts?.Cancel();
                _cts?.Dispose();
                _client?.Disconnect();
            }
        }
    }
}
