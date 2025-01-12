
using TopNetwork.Core;
using TopNetwork.Services.MessageBuilder;

namespace WPFTasks.Core.Models.TicTacToe.MessageBuilder
{
    public class TieGameRequestData : IMsgSourceData
    {
        public string MessageType => MsgType;
        public static string MsgType => "TieGameRequest";
    }

    public class TieGameRequestMsgBuilder : IMessageBuilder<TieGameRequestData>
    {
        public Message BuildMsg()
        {
            return new()
            {
                MessageType = TieGameRequestData.MsgType,
            };
        }

        public static TieGameRequestData Parse(Message msg)
        {
            if (msg.MessageType != TieGameRequestData.MsgType)
                throw new InvalidOperationException("Incorrect message type.");

            return new TieGameRequestData();
        }
    }
}
