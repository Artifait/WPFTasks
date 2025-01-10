
using TopNetwork.Core;
using TopNetwork.Services.MessageBuilder;

namespace WPFTasks.Core.Models.Chat.MessageBuilder
{
    public class ChatMessageData : IMsgSourceData
    {
        public string Payload { get; set; } = string.Empty;

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

        public Message BuildMsg()
        {
            return new()
            {
                MessageType = _data.MessageType,
                Payload = _data.Payload,
            };
        }

        public static ChatMessageData Parse(Message msg)
        {
            if (msg.MessageType != ChatMessageData.MsgType)
                throw new InvalidOperationException("Incorrect message type.");

            return new()
            {
                Payload = msg.Payload,
            };
        }
    }
}
