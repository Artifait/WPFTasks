
using TopNetwork.Core;
using TopNetwork.Services.MessageBuilder;

namespace WPFTasks.Core.Models.TicTacToe.MessageBuilder
{
    public class GameStartedData : IMsgSourceData
    {
        public char YourSymbol;

        public string MessageType => MsgType;
        public static string MsgType => "GameStartedNotification";
    }
    public class GameStartedMsgBuilder : IMessageBuilder<GameStartedData>
    {
        private GameStartedData _data = new();

        public GameStartedMsgBuilder SetUserSymbol(char simbol)
        {
            _data.YourSymbol = simbol;
            return this;
        }

        public Message BuildMsg()
        {
            return new()
            {
                MessageType = _data.MessageType,
                Payload = $"{_data.YourSymbol}"
            };
        }

        public static GameStartedData Parse(Message msg)
        {
            if (msg.MessageType != GameStartedData.MsgType)
                throw new InvalidOperationException("Incorrect message type.");

            return new GameStartedData()
            {
                YourSymbol = msg.Payload[0]
            };
        }
    }
}
