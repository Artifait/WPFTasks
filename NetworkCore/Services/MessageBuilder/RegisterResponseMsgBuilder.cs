
using TopNetwork.Core;

namespace TopNetwork.Services.MessageBuilder
{
    public class RegisterResponseData : IMsgSourceData
    {
        public string Payload { get; set; } = string.Empty;

        public string MessageType => MsgType;
        public static string MsgType => "RegisterResponse";
    }

    public class RegisterResponseMsgBuilder : IMessageBuilder<RegisterResponseData>
    {
        private RegisterResponseData _data = new();

        public RegisterResponseMsgBuilder SetExplanatoryMsg(string payload)
        {
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

        public static RegisterResponseData Parse(Message msg)
        {
            if (msg.MessageType != RegisterResponseData.MsgType)
                throw new InvalidOperationException("Incorrect message type.");

            return new()
            {
                Payload = msg.Payload
            };
        }
    }
}
