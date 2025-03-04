using TopNetwork.Core;
using TopNetwork.Services.MessageBuilder;

namespace WPFTasks.Core.Models.RockPaperScissors.MessageBuilder
{
    public class EndGameRequestData : IMsgSourceData
    {
        public string MessageType => MsgType;
        public static string MsgType => "RpsEndGameRequest";
    }

    public class EndGameRequestMsgBuilder : IMessageBuilder<EndGameRequestData>
    {
        public Message BuildMsg()
        {
            return new Message
            {
                MessageType = EndGameRequestData.MsgType,
            };
        }

        public static EndGameRequestData Parse(Message msg)
        {
            if (msg.MessageType != EndGameRequestData.MsgType)
                throw new InvalidOperationException("Incorrect message type.");

            return new EndGameRequestData();
        }
    }
}
