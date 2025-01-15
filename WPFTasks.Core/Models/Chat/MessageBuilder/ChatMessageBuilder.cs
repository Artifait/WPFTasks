
using TopNetwork.Core;
using TopNetwork.Services.MessageBuilder;

namespace WPFTasks.Core.Models.Chat.MessageBuilder
{
    public class ChatMessageData : IMsgSourceData
    {
        public string Payload { get; set; } = string.Empty;
        public Dictionary<string, string> Headers { get; set; } = [];

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

        public ChatMessageBuilder AddHeader(string name, string payload)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(payload);

            _data.Headers.Add(name, payload);
            return this;
        }

        public Message BuildMsg()
        {
            return new()
            {
                Headers = _data.Headers,
                Payload = _data.Payload,
                MessageType = _data.MessageType,
            };
        }

        public static ChatMessageData Parse(Message msg)
        {
            if (msg.MessageType != ChatMessageData.MsgType)
                throw new InvalidOperationException("Incorrect message type.");

            return new()
            {
                Headers = msg.Headers,
                Payload = msg.Payload,
            };
        }
    }
}
