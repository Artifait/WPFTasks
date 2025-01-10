
using TopNetwork.Core;
using TopNetwork.Services.MessageBuilder;

namespace WPFTasks.Core.Models.TicTacToe.MessageBuilder
{
    public class EndGameRequestData : IMsgSourceData
    {
        public string MessageType => MsgType;
        public static string MsgType => "EndGameRequest";
    }

    public class EndGameRequestMsgBuilder : IMessageBuilder<EndGameRequestData>
    {
        public Message BuildMsg()
        {
            return new()
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
