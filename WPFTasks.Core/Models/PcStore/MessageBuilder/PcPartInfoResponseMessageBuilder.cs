
using TopNetwork.Core;
using TopNetwork.Services.MessageBuilder;

namespace WPFTasks.Core.Models.PcStore.MessageBuilder
{
    public class PcPartInfoResponseData : IMsgSourceData
    {
        public string Title { get; set; } = string.Empty;
        public int Price { get; set; } = -1;
        public string MessageType => MsgType;
        public static string MsgType => "PcPartInfoResponse";
    }

    public class PcPartInfoResponseMessageBuilder : IMessageBuilder<PcPartInfoResponseData>
    {
        private PcPartInfoResponseData _data = new();

        public PcPartInfoResponseMessageBuilder SetTitle(string title)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(title);
            _data.Title = title;

            return this;
        }

        public PcPartInfoResponseMessageBuilder SetPrice(int price)
        {
            _data.Price = price;
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

        public static PcPartInfoResponseData Parse(Message msg)
        {
            if (msg.MessageType != PcPartInfoResponseData.MsgType)
                throw new InvalidOperationException("Incorrect message type.");

            return new()
            {
                Title = msg.Payload
            };
        }
    }
}
