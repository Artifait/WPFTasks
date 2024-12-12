
using TopNetwork.Core;

using MsgT = WPFTasks.Core.Models.Currency.CurrencyMsgBuilder.Types;
using MsgH = WPFTasks.Core.Models.Currency.CurrencyMsgBuilder.Headers;
using MsgBuilder = WPFTasks.Core.Models.Currency.CurrencyMsgBuilder;

namespace WPFTasks.Core.Models.Currency
{
    public class CurrencyClient
    {
        private static readonly Func<MsgT, string> GetMsgTStr = MsgBuilder.GetMessageTypeStr;
        private static readonly Func<MsgH, string> GetMsgHStr = MsgBuilder.GetHeaderStr;


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
        private ClientHandlerBase _handlers;
        /// <summary>
        /// 1) string - FromCurrency
        /// 2) string - ToCurrency
        /// 3) double - rate
        /// </summary>
        public event Action<string, string, double>? OnGetCurrencyRateMsg;
        public event Action<bool>? OnAuthenticated;
        public event Action<string>? OnGetErrorMsg;

        private void InitClientHandlers()
        {
            _handlers = new();

            _handlers.AddHandlerForMessageType(
                GetMsgTStr(MsgT.CurrencyRateResult),
                msg =>
                {
                    try
                    {
                        OnGetCurrencyRateMsg?.Invoke(
                            msg.Headers["FromCurrency"],
                            msg.Headers["ToCurrency"],
                            double.Parse(msg.Payload)
                        );
                    }
                    catch (Exception ex) { OnGetErrorMsg?.Invoke($"Не смогли распарсить ответ от сервера.\nОшибка: {ex.Message}."); }
                }
            );

            _handlers.AddHandlerForMessageType(
                GetMsgTStr(MsgT.AuthenticationResult),
                msg =>
                {
                    try
                    {
                        if (bool.Parse(msg.Headers[GetMsgHStr(MsgH.IsAuthed)]))
                        {
                            Authenticated = true;
                        }
                        else
                        {
                            OnGetErrorMsg?.Invoke($"Ошибка аутентификации: {msg.Payload}");
                        }
                    }
                    catch(Exception ex)
                    {
                        OnGetErrorMsg?.Invoke(ex.Message);
                    }
                }
            );

            _handlers.AddHandlerForMessageType(
                GetMsgTStr(MsgT.Error),
                msg =>
                {
                    OnGetErrorMsg?.Invoke(msg.Payload);
                }
            );
        }
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

            _client.OnAcceptedMessage += _handlers.HandleMessage;
            _client.OnDisconnected += () => Authenticated = false;

            _ = _client.StartListen(_cts.Token);

            _ = Authentication(login, password);
        }
        public async Task Authentication(string login, string password)
        {
            if (_client == null) throw new NullReferenceException("Не инициализированный клиент.");

            Message request = CurrencyMsgBuilder.CreateAuthenticationRequest(login, password);

            await _client.SendMessageAsync(request);
        }
        public async Task RequestCurrencyRate(string fromCurrency, string toCurrency)
        {
            if (_client == null) throw new NullReferenceException("Не инициализированный клиент.");

            await _client.SendMessageAsync(CurrencyMsgBuilder.CreateCurrencyRateRequest(fromCurrency, toCurrency));
        }

        public async Task TryDisconnect()
        {
            if(_client?.IsConnected ?? false)
            {
                await _client.SendMessageAsync(MsgBuilder.CreateCloseSessionRequest());

                _cts?.Cancel();
                _cts?.Dispose();
                _client?.Disconnect();
            }
        }
    }
}
