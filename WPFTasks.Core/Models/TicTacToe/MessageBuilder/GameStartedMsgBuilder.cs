
using TopNetwork.Core;
using TopNetwork.Services.MessageBuilder;

namespace WPFTasks.Core.Models.TicTacToe.MessageBuilder
{
    public class GameStartedData : IMsgSourceData
    {
        public string MessageType => MsgType;
        public static string MsgType => "GameStartedNotification";
    }
    public class GameStartedMsgBuilder : IMessageBuilder<GameStartedData>
    {
        public Message BuildMsg()
        {
            return new()
            {
                MessageType = GameStartedData.MsgType,
            };
        }

        public static GameStartedData Parse(Message msg)
        {
            if (msg.MessageType != GameStartedData.MsgType)
                throw new InvalidOperationException("Incorrect message type.");

            return new GameStartedData();
        }
    }
}
