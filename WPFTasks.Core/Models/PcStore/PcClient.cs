
using TopNetwork.Services.MessageBuilder;
using WPFTasks.Core.Models.Core;
using WPFTasks.Core.Models.PcStore.MessageBuilder;

namespace WPFTasks.Core.Models.PcStore
{
    public class PcClient : BaseClient
    {
        public event Action<EndSessionNotificationData>? OnEndSession;
        public event Action<AuthenticationResponseData>? OnAuthenticationResponse;
        public event Action<PcPartInfoResponseData>? OnPcPartInfoResponse;

        public PcClient() : base() { }


        protected override void RegisterMessageBuilders()
        {
            MessageBuilderService
                .Register(() => new PcPartInfoRequestMessageBuilder())
                .Register(() => new AuthenticationRequestMessageBuilder())
                .Register(() => new CloseSessionRequestMessageBuilder());
        }

        protected override void RegisterMessageHandlers()
        {
            Handlers
                .AddHandlerForMessageType(PcPartInfoResponseData.MsgType, async msg =>
                {
                    try
                    {
                        OnPcPartInfoResponse?.Invoke(PcPartInfoResponseMessageBuilder.Parse(msg));
                    }
                    catch
                    {
                        InvokeOnErroreOnClient($"Error parsing response: {msg}");
                    }
                    return null;
                })
                .AddHandlerForMessageType(EndSessionNotificationData.MsgType, async msg =>
                {
                    Disconnect();
                    try
                    {
                        OnEndSession?.Invoke(EndSessionNotificationMessageBuilder.Parse(msg));
                    }
                    catch
                    {
                        InvokeOnErroreOnClient($"Error parsing response: {msg}");
                    }
                    return null;
                })
                .AddHandlerForMessageType(AuthenticationResponseData.MsgType, async msg =>
                {
                    try
                    {
                        OnAuthenticationResponse?.Invoke(AuthenticationResponseMessageBuilder.Parse(msg));
                    }
                    catch
                    {
                        InvokeOnErroreOnClient($"Error parsing response: {msg}");
                    }
                    return null;
                })
                .AddHandlerForMessageType(ErroreData.MsgType, async msg =>
                {
                    try
                    {
                        InvokeOnErroreFromServer(ErroreMessageBuilder.Parse(msg));
                    }
                    catch
                    {
                        InvokeOnErroreOnClient($"Error parsing response: {msg}");
                    }
                    return null;
                })
                .AddHandlerForMessageType(ServerOverloadedNotificationData.MsgType, async msg =>
                {
                    InvokeOnServerOverloaded();
                    return null;
                });
        }

        public async Task SendAuthRequest(string login, string password)
        {
            await SendMessageAsync<AuthenticationRequestMessageBuilder, AuthenticationRequestData>(
                builder => builder.SetLogin(login).SetPassword(password)
            );
        }

        public async Task SendPcPartInfoRequest(string titleOfPart)
        {
            await SendMessageAsync<PcPartInfoRequestMessageBuilder, PcPartInfoRequestData>(
                builder => builder.SetPayload(titleOfPart)
            );
        }

        public async Task SendCloseSessionRequest()
        {
            await SendMessageAsync<CloseSessionRequestMessageBuilder, CloseSessionRequestData>();
        }
    }
}
