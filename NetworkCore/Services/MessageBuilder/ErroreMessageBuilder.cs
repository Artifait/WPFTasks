
using TopNetwork.Core;

namespace TopNetwork.Services.MessageBuilder
{
    public class ErroreData : IMsgSourceData
    {
        public string Payload = "Ошибка";
        public string MessageType => "Errore";
    }

    public class ErroreMessageBuilder : IMessageBuilder<ErroreData>
    {
        private ErroreData _data = new();

        public ErroreMessageBuilder SetPayload(string payload)
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
                Payload = _data.Payload
            };
        }

        public ErroreData Parse(Message msg)
        {
            if (msg.MessageType != _data.MessageType)
                throw new InvalidOperationException("Incorrect message type.");

            return new()
            {
                Payload = msg.Payload
            };
        }
    }
}
