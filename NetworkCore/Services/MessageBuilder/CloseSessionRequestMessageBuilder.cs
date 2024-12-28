
using TopNetwork.Core;

namespace TopNetwork.Services.MessageBuilder
{
    public class CloseSessionRequestData : IMsgSourceData
    {
        public string MessageType => MsgType;
        public static string MsgType => "CloseSessionRequest";
    }

    public class CloseSessionRequestMessageBuilder : IMessageBuilder<CloseSessionRequestData>
    {
        private CloseSessionRequestData _data = new();

        public Message BuildMsg()
        {
            return new()
            {
                MessageType = _data.MessageType,
            };
        }

        public CloseSessionRequestData Parse(Message msg)
        {
            if (msg.MessageType != _data.MessageType)
                throw new InvalidOperationException("Incorrect message type.");

            return new();
        }
    }
}
