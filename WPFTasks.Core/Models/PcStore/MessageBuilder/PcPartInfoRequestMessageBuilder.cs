
using TopNetwork.Core;
using TopNetwork.Services.MessageBuilder;

namespace WPFTasks.Core.Models.PcStore.MessageBuilder
{
    public class PcPartInfoRequestData : IMsgSourceData
    {
        public string Payload { get; set; } = string.Empty;

        public string MessageType => MsgType;
        public static string MsgType => "PcPartInfoRequest";
    }

    public class PcPartInfoRequestMessageBuilder : IMessageBuilder<PcPartInfoRequestData>
    {
        private PcPartInfoRequestData _data = new();

        public PcPartInfoRequestMessageBuilder SetPayload(string payload)
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

        public static PcPartInfoRequestData Parse(Message msg)
        {
            if (msg.MessageType != PcPartInfoRequestData.MsgType)
                throw new InvalidOperationException("Incorrect message type.");

            return new()
            {
                Payload = msg.Payload
            };
        }
    }

}
