
using TopNetwork.RequestResponse;
using TopNetwork.Services.MessageBuilder;
using WPFTasks.Core.Models.Currency.MessageBuilder;

namespace WPFTasks.Core.Models.Currency
{
    public class CurrencyClient
    {
        // Регистрация всех фабрик для типов сообщений отправляемых клиентом 
        private static MessageBuilderService _msgService = new MessageBuilderService()
                    .Register(() => new CurrencyRequestMessageBuilder())
                    .Register(() => new AuthenticationRequestMessageBuilder())
                    .Register(() => new CloseSessionRequestMessageBuilder());

        private RrClientHandlerBase _handlers;
        private RrClient _client;

        public event Action OnEndSession;
        public event Action OnConnectionLost;
        public event Action<AuthenticationResponseData> OnAuthenticationResponse;
        public event Action<CurrencyResponseData> OnCurrencyResponse;
        public event Action<string> OnErroreOnClient;
        public event Action<ErroreData> OnErroreFromServer;
        public event Action OnServerOverloaded;

        public bool IsInitialized => _client?.IsInitialized ?? false;
        public bool IsConnected => _client?.IsConnected ?? false;
        public bool IsAuth { get; set; }

        public CurrencyClient()
        {
            _handlers = new RrClientHandlerBase()
                .AddHandlerForMessageType(CurrencyResponseData.MsgType, async msg =>
                {
                    try {
                        OnCurrencyResponse?.Invoke(CurrencyResponseMessageBuilder.Parse(msg));
                    }
                    catch (Exception ex) {
                        OnErroreOnClient?.Invoke($"Ошибка при парсинге ответа: {msg}");
                    }

                    return null;
                })
                .AddHandlerForMessageType(EndSessionNotificationData.MsgType, async msg =>
                {
                    _client?.Disconnect();
                    IsAuth = false;

                    OnEndSession?.Invoke();
                    return null;
                })
                .AddHandlerForMessageType(AuthenticationResponseData.MsgType, async msg =>
                {
                    try {
                        var response = AuthenticationResponseMessageBuilder.Parse(msg);
                        IsAuth = response.IsAuthenticated;

                        OnAuthenticationResponse?.Invoke(response);
                    }
                    catch (Exception ex) {
                        OnErroreOnClient?.Invoke($"Ошибка при парсинге ответа: {msg}");
                    }

                    return null;
                })
                .AddHandlerForMessageType(ErroreData.MsgType, async msg =>
                {
                    try {
                        OnErroreFromServer?.Invoke(ErroreMessageBuilder.Parse(msg));
                    }
                    catch (Exception ex) {
                        OnErroreOnClient?.Invoke($"Ошибка при парсинге ответа: {msg}");
                    }

                    return null;
                })
                .AddHandlerForMessageType(ServerOverloadedNotificationData.MsgType, async msg =>
                {
                    OnServerOverloaded?.Invoke();
                    return null;
                });

            _client = new RrClient(_handlers);

            _client.ServiceRegistry
                .Register(_msgService);

            _client.OnConnectionLost += () => OnConnectionLost?.Invoke();
        }

        #region Requests
        public async Task SendAuthRequest(string login, string password)
        {
            var msg = _msgService
                .BuildMessage<AuthenticationRequestMessageBuilder, AuthenticationRequestData>(
                    builder => builder
                        .SetLogin(login)
                        .SetPassword(password)
                );

            await _client.SendMessageWithoutResponseAsync(msg);
        }

        public async Task SendCurrencyRequest(string fromCurrency, string toCurrency)
        {
            var msg = _msgService
                .BuildMessage<CurrencyRequestMessageBuilder, CurrencyRequestData>(
                    builder => builder
                        .SetFromCurrency(fromCurrency)
                        .SetToCurrency(toCurrency)
                );

            await _client.SendMessageWithoutResponseAsync(msg);
        }

        public async Task SendCloseSessionRequest()
        {
            var msg = _msgService.BuildMessage<CloseSessionRequestMessageBuilder, CloseSessionRequestData>(null);

            await _client.SendMessageWithoutResponseAsync(msg);
        }
        #endregion

        public void Connect(string IpServer, int port)
            => _client.Connect(IpServer, port);
        public void Disconnect()
            => _client.Disconnect();
    }
}