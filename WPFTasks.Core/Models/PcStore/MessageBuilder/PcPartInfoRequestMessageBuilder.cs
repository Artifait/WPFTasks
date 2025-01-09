
using TopNetwork.Core;
using TopNetwork.Services.MessageBuilder;

namespace WPFTasks.Core.Models.PcStore.MessageBuilder
{
    public class PcPartInfoRequestData : IMsgSourceData
    {
        public string Title { get; set; } = string.Empty;

        public string MessageType => MsgType;
        public static string MsgType => "PcPartInfoRequest";
    }

    public class PcPartInfoRequestMessageBuilder : IMessageBuilder<PcPartInfoRequestData>
    {
        private PcPartInfoRequestData _data = new();

        public PcPartInfoRequestMessageBuilder SetPayload(string payload)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(payload);
            _data.Title = payload;

            return this;
        }

        public Message BuildMsg()
        {
            return new()
            {
                MessageType = _data.MessageType,
                Payload = _data.Title
            };
        }

        public static PcPartInfoRequestData Parse(Message msg)
        {
            if (msg.MessageType != PcPartInfoRequestData.MsgType)
                throw new InvalidOperationException("Incorrect message type.");

            return new()
            {
                Title = msg.Payload
            };
        }
    }

}
