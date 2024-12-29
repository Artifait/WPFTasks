
using TopNetwork.Core;
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

        public bool IsInitialized => _client?.IsInitialized ?? false;
        public bool IsConnected => _client?.IsConnected ?? false;
        public bool IsAuth { get; set; }

        public event Action? OnEndSession;
        public event Action? OnConnectionLost;
        public event Action<AuthenticationResponseData> OnAuthenticationResponse;
        public event Action<CurrencyResponseData> OnCurrencyResponse;
        public event Action<string> OnErrore;

        public CurrencyClient()
        {
            _handlers = new RrClientHandlerBase()
                .AddHandlerForMessageType(CurrencyResponseData.MsgType, async msg =>
                {
                    try {
                        OnCurrencyResponse?.Invoke(CurrencyResponseMessageBuilder.Parse(msg));
                    }
                    catch (Exception ex) {
                        OnErrore?.Invoke($"Ошибка при парсинге ответа: {msg}");
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
                        OnErrore?.Invoke($"Ошибка при парсинге ответа: {msg}");
                    }

                    return null;
                });

            _client = new RrClient(_handlers);

            _client.ServiceRegistry.
                Register(_msgService);

            _client.OnConnectionLost += OnConnectionLost;
        }

        #region AuthRequest
        public async Task<AuthenticationResponseData?> SendAuthRequestWithResponse(string login, string password)
        {
            var msg = BuildAuthRequestMsg(login, password);

            try
            {
                var response = await _client.SendMessageWithResponseAsync(msg);
                ArgumentNullException.ThrowIfNull(response);

                var data = AuthenticationResponseMessageBuilder.Parse(response);
                IsAuth = data.IsAuthenticated;

                return data;
            }
            catch (Exception ex)
            {
                OnErrore?.Invoke($"Ошибка при парсинге ответа:\n{msg}");
            }
            return null;
        }

        public async Task SendAuthRequest(string login, string password)
        {
            var msg = BuildAuthRequestMsg(login, password);

            await _client.SendMessageWithoutResponseAsync(msg);
        }
        private Message BuildAuthRequestMsg(string login, string password)
        {
            return _msgService
                .BuildMessage<AuthenticationRequestMessageBuilder, AuthenticationRequestData>(
                        builder => builder
                            .SetLogin(login)
                            .SetPassword(password)
                );
        }
        #endregion

        #region CurrencyRequest
        public async Task<IMsgSourceData?> SendCurrencyRequestWithResponse(string fromCurrency, string toCurrency)
        {
            var msg = BuildCurrencyRequestMsg(fromCurrency, toCurrency);
            try
            {
                var response = await _client.SendMessageWithResponseAsync(msg);
                ArgumentNullException.ThrowIfNull(response);

                if(response.MessageType == AuthenticationResponseData.MsgType)
                    return CurrencyResponseMessageBuilder.Parse(response);

                if (response.MessageType == ErroreData.MsgType)
                    return ErroreMessageBuilder.Parse(msg);

                throw new Exception($"Не удалось распознать сообщение, его тип: {response.MessageType}.");
            }
            catch (Exception ex)
            {
                OnErrore?.Invoke($"[Server]: Ошибка при парсинге ответа:\n{msg}");
            }
            return null;
        }

        public async Task SendCurrencyRequest(string fromCurrency, string toCurrency)
        {
            var msg = BuildCurrencyRequestMsg(fromCurrency, toCurrency);

            await _client.SendMessageWithoutResponseAsync(msg);
        }

        private Message BuildCurrencyRequestMsg(string fromCurrency, string toCurrency)
        {
            return _msgService
                .BuildMessage<CurrencyRequestMessageBuilder, CurrencyRequestData>(
                        builder => builder
                            .SetFromCurrency(fromCurrency)
                            .SetToCurrency(toCurrency)
                );
        }
        #endregion

        #region CloseSessionRequest
        public async Task<EndSessionNotificationData?> SendCloseSessionRequestWithResponse(string fromCurrency, string toCurrency)
        {
            var msg = BuildCloseSessionRequestMsg();

            try
            {
                var response = await _client.SendMessageWithResponseAsync(msg);
                ArgumentNullException.ThrowIfNull(response);

                return EndSessionNotificationMessageBuilder.Parse(response);
            }
            catch (Exception ex)
            {
                OnErrore?.Invoke($"Ошибка при парсинге ответа:\n{msg}");
            }
            return null;
        }

        public async Task SendCloseSessionRequest()
        {
            var msg = BuildCloseSessionRequestMsg();

            await _client.SendMessageWithoutResponseAsync(msg);
        }

        private Message BuildCloseSessionRequestMsg()
            => _msgService.BuildMessage<CloseSessionRequestMessageBuilder, CloseSessionRequestData>(null);
        
        #endregion

        public void Connect(string IpServer, int port)
            => _client.Connect(IpServer, port);
        public void Disconnect()
            => _client.Disconnect();
    }
}