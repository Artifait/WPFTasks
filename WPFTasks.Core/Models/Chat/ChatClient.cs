
using TopNetwork.Core;
using TopNetwork.Services.MessageBuilder;
using WPFTasks.Core.Models.Chat.MessageBuilder;
using WPFTasks.Core.Models.Core;

namespace WPFTasks.Core.Models.Chat
{
    public class ChatClient : BaseClient
    {
        public event Action<ChatUpdatedData>? OnChatUpdated;
        public event Action<ChatHistoryResponseData>? OnGetChatHistory;
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
                .AddHandlerForMessageType(EndSessionNotificationData.MsgType, async msg =>
                {
                    Disconnect();

                    return await SafeWrapperForHandler(msg, async msg
                        => OnEndSession?.Invoke(EndSessionNotificationMessageBuilder.Parse(msg)));
                })
                .AddHandlerForMessageType(AuthenticationResponseData.MsgType, async msg =>
                {
                    return await SafeWrapperForHandler(msg, async msg
                        => OnAuthenticationResponse?.Invoke(AuthenticationResponseMessageBuilder.Parse(msg)));
                })
                .AddHandlerForMessageType(ErroreData.MsgType, async msg =>
                {
                    return await SafeWrapperForHandler(msg, async msg 
                        => InvokeOnErroreFromServer(ErroreMessageBuilder.Parse(msg)));
                })
                .AddHandlerForMessageType(ServerOverloadedNotificationData.MsgType, async msg =>
                {
                    InvokeOnServerOverloaded();
                    return null;
                });
        }

        public async Task SendChatHistoryRequest(string chatId)
        {
            await SendMessageAsync<ChatHistoryRequestMsgBuilder, ChatHistoryRequestData>(
                builder => builder.SetChat(chatId)
            );
        }

        public async Task SendAuthRequest(string login, string password)
        {
            await SendMessageAsync<AuthenticationRequestMessageBuilder, AuthenticationRequestData>(
                builder => builder.SetLogin(login).SetPassword(password)
            );
        }

        public async Task SendChatMessage(string message, string chatId)
        {
            await SendMessageAsync<ChatMessageBuilder, ChatMessageData>(builder => builder
                .SetPayload(message)
                .SetChatId(chatId)
            );
        }

        public async Task SendCloseSessionRequest()
        {
            await SendMessageAsync<CloseSessionRequestMessageBuilder, CloseSessionRequestData>();
        }

        private async Task<Message?> SafeWrapperForHandlerWithAnswer(Message msg, Func<Message, Task<Message?>> handler)
        {
            try
            {
                return await handler?.Invoke(msg);
            }
            catch (Exception ex)
            {
                InvokeOnErroreOnClient($"Error parsing response of type: {msg.MessageType} - {ex.Message}");
                return null;
            }
        }

        private async Task<Message?> SafeWrapperForHandler(Message msg, Func<Message, Task> handler)
        {
            try
            {
                await handler?.Invoke(msg);
            }
            catch (Exception ex)
            {
                InvokeOnErroreOnClient($"Error parsing response of type: {msg.MessageType} - {ex.Message}");
            }

            return null;
        }
    }
}
