
using TopNetwork.Core;

namespace TopNetwork.Services.MessageBuilder
{
    public class DefaultData : IMsgSourceData
    {
        public string Payload = string.Empty;
        public string MessageType = string.Empty;
        public Dictionary<string, string> Headers = [];

        string IMsgSourceData.MessageType => MessageType;
    }

    public class DefaultMessageBuilder : IMessageBuilder<DefaultData>
    {
        private DefaultData _data = new();

        public DefaultMessageBuilder SetPayload(string payload)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(payload);

            _data.Payload = payload;
            return this;
        }

        public DefaultMessageBuilder AddHeader(string name, string payload)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            ArgumentException.ThrowIfNullOrWhiteSpace(payload);

            _data.Headers.Add(name, payload);
            return this;
        }

        public DefaultMessageBuilder SetMessageType(string type)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(type);

            _data.MessageType = type;
            return this;
        }
        public Message BuildMsg()
        {
            return new Message() 
            { 
                Headers = _data.Headers,
                MessageType = _data.MessageType,
                Payload = _data.Payload
            };
        }

        public DefaultData Parse(Message msg)
        {
            return new DefaultData() 
            { 
                Payload = msg.Payload,
                Headers = msg.Headers,
                MessageType = msg.MessageType
            };
        }
    }
}
