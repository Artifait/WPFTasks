

using TopNetwork.Core;
using TopNetwork.Services.MessageBuilder;

namespace WPFTasks.Core.Models.TicTacToe.MessageBuilder
{
    public class PlayerTurnData : IMsgSourceData
    {
        public string MessageType => MsgType;
        public static string MsgType => "PlayerTurnNotification";
    }

    public class PlayerTurnMsgBuilderMsgBuilder : IMessageBuilder<PlayerTurnData>
    {
        public Message BuildMsg()
        {
            return new()
            {
                MessageType = PlayerTurnData.MsgType,
            };
        }

        public static PlayerTurnData Parse(Message msg)
        {
            if (msg.MessageType != PlayerTurnData.MsgType)
                throw new InvalidOperationException("Incorrect message type.");

            return new PlayerTurnData();
        }
    }
}
