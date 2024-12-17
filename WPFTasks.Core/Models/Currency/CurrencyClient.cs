
using TopNetwork.Core;

using MsgT = WPFTasks.Core.Models.Currency.CurrencyMsgBuilder.Types;
using MsgH = WPFTasks.Core.Models.Currency.CurrencyMsgBuilder.Headers;
using MsgBuilder = WPFTasks.Core.Models.Currency.CurrencyMsgBuilder;

namespace WPFTasks.Core.Models.Currency
{
    public class CurrencyClient : DefaultClient
    {
        public event Action<string, string, double>? OnGetCurrencyRateMsg;
        public event Action<bool, string>? OnAuthenticated;
        public event Action OnGetEndSessionNotification;

        public CurrencyClient() : base() { }

        public async Task AuthenticateAsync(string login, string password)
        {
            var authMessage = MsgBuilder.CreateAuthenticationRequest(login, password);
            await SendMessageAsync(authMessage);
        }

        public async Task RequestCurrencyRateAsync(string fromCurrency, string toCurrency)
        {
            var rateRequest = MsgBuilder.CreateCurrencyRateRequest(fromCurrency, toCurrency);
            await SendMessageAsync(rateRequest);
        }

        protected override void InitializeHandlers()
        {
            Handlers.AddHandlerForMessageType(
                MsgBuilder.GetMessageTypeStr(MsgT.CurrencyRateResult),
                msg =>
                {
                    try
                    {
                        if (bool.Parse(msg.Headers[MsgBuilder.GetHeaderStr(MsgH.IsSuccessfulOperation)]))
                        {
                            OnGetCurrencyRateMsg?.Invoke(
                                msg.Headers["FromCurrency"],
                                msg.Headers["ToCurrency"],
                                double.Parse(msg.Payload));
                        }
                        else
                        {
                            OnErrorOccurred?.Invoke(msg.Payload);
                        }
                    }
                    catch (Exception ex)
                    {
                        OnErrorOccurred?.Invoke($"Ошибка парсинга: {ex.Message}");
                    }
                });

            Handlers.AddHandlerForMessageType(
                MsgBuilder.GetMessageTypeStr(MsgT.AuthenticationResult),
                msg =>
                {
                    try
                    {
                        if (bool.Parse(msg.Headers[MsgBuilder.GetHeaderStr(MsgH.IsAuthed)]))
                        {
                            OnAuthenticated?.Invoke(true, msg.Payload);
                            Authenticated = true;
                        }
                        else
                        {
                            OnAuthenticated?.Invoke(false, $"Ошибка аутентификации: {msg.Payload}");
                        }
                    }
                    catch (Exception ex)
                    {
                        OnErrorOccurred?.Invoke($"Ошибка аутентификации: {ex.Message}");
                    }
                });

            Handlers.AddHandlerForMessageType(
                MsgBuilder.GetMessageTypeStr(MsgT.EndSessionNotification),
                msg =>
                {
                    OnGetEndSessionNotification?.Invoke();
                    CurrentDispatcher.Invoke(() => Authenticated = false);
                });
        }

        protected override Message? CreateCloseSessionMessage()
            => MsgBuilder.CreateCloseSessionRequest();
    }
}