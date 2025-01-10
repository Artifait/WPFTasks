
using TopNetwork.Services.MessageBuilder;
using WPFTasks.Core.Models.Chat.MessageBuilder;
using WPFTasks.Core.Models.Core;

namespace WPFTasks.Core.Models.Chat
{
    public class ChatClient : BaseClient
    {
        public event Action<ChatMessageData>? OnReceivedChatMessage;
        public event Action<EndSessionNotificationData>? OnEndSession;
        public event Action<AuthenticationResponseData>? OnAuthenticationResponse;

        protected override void RegisterMessageBuilders()
        {
            MessageBuilderService
                .Register(() => new ChatMessageBuilder())
                .Register(() => new AuthenticationRequestMessageBuilder())
                .Register(() => new CloseSessionRequestMessageBuilder());
        }

        protected override void RegisterMessageHandlers()
        {
            Handlers
                .AddHandlerForMessageType(ChatMessageData.MsgType, async msg =>
                {
                    try
                    {
                        OnReceivedChatMessage?.Invoke(ChatMessageBuilder.Parse(msg));
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

        public async Task SendChatMessage(string message)
        {
            await SendMessageAsync<ChatMessageBuilder, ChatMessageData>(
                builder => builder.SetPayload(message)
            );
        }

        public async Task SendCloseSessionRequest()
        {
            await SendMessageAsync<CloseSessionRequestMessageBuilder, CloseSessionRequestData>();
        }
    }
}
