using TopNetwork.Core;
using TopNetwork.Services.MessageBuilder;

namespace WPFTasks.Core.Models.RockPaperScissors.MessageBuilder
{
    public class RoundResultData : IMsgSourceData
    {
        public string Result { get; set; } = string.Empty;
        public string MessageType => MsgType;
        public static string MsgType => "RpsRoundResult";
    }

    public class RoundResultMsgBuilder : IMessageBuilder<RoundResultData>
    {
        private RoundResultData _data = new();

        public RoundResultMsgBuilder SetResult(string result)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(result, nameof(result));
            _data.Result = result;
            return this;
        }

        public Message BuildMsg()
        {
            return new Message
            {
                MessageType = _data.MessageType,
                Payload = _data.Result
            };
        }

        public static RoundResultData Parse(Message msg)
        {
            if (msg.MessageType != RoundResultData.MsgType)
                throw new InvalidOperationException("Incorrect message type.");

            return new RoundResultData
            {
                Result = msg.Payload
            };
        }
    }
}
