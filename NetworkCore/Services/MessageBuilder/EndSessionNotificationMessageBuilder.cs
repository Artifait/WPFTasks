
using TopNetwork.Core;

namespace TopNetwork.Services.MessageBuilder
{
    public class EndSessionNotificationData : IMsgSourceData
    {
        public string MessageType => "EndSessionNotification";
        public string Payload = "Ваша сессия завершена...";
    }

    public class EndSessionNotificationMessageBuilder : IMessageBuilder<EndSessionNotificationData>
    {
        private EndSessionNotificationData _data = new();

        public EndSessionNotificationMessageBuilder SetPayload(string payload)
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

        public EndSessionNotificationData Parse(Message msg)
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
