
using TopNetwork.Core;
using TopNetwork.Services.MessageBuilder;

namespace WPFTasks.Core.Models.Chat.MessageBuilder
{
    public class ChatMessageData : IMsgSourceData
    {
        public string Payload { get; set; } = string.Empty;
        public string ChatId { get; set; } = string.Empty;

        public string MessageType => MsgType;
        public static string MsgType => "ChatMessage";
    }

    public class ChatMessageBuilder : IMessageBuilder<ChatMessageData>
    {
        private ChatMessageData _data = new();

        public ChatMessageBuilder SetPayload(string payload)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(payload);
            _data.Payload = payload;

            return this;
        }

        public ChatMessageBuilder SetChatId(string chatId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(chatId);
            _data.ChatId = chatId;

            return this;
        }

        public Message BuildMsg()
        {
            return new()
            {
                Payload = $"{_data.ChatId}:{_data.Payload}",
                MessageType = _data.MessageType,
            };
        }

        public static ChatMessageData Parse(Message msg)
        {
            if (msg.MessageType != ChatMessageData.MsgType)
                throw new InvalidOperationException("Incorrect message type.");

            var parts = msg.Payload.Split(':');

            return new()
            {
                ChatId = parts[0],
                Payload = parts[1],
            };
        }
    }
}
