
using TopNetwork.Core;
using TopNetwork.Services.MessageBuilder;
using WPFTasks.Core.Models.TicTacToe.Services;

namespace WPFTasks.Core.Models.TicTacToe.MessageBuilder
{
    public class FindGameRequestData : IMsgSourceData
    {
        public GameTypes GameType { get; set; }
        public string MessageType => MsgType;
        public static string MsgType => "FindGameRequest";
    }

    public class FindGameRequestMsgBuilder : IMessageBuilder<FindGameRequestData>
    {
        private FindGameRequestData _data = new();

        public FindGameRequestMsgBuilder SetGameType(GameTypes gameType)
        {
            _data.GameType = gameType;
            return this;
        }

        public Message BuildMsg()
        {
            return new()
            {
                MessageType = _data.MessageType,
                Payload = _data.GameType.ToString()
            };
        }

        public static FindGameRequestData Parse(Message msg)
        {
            if (msg.MessageType != FindGameRequestData.MsgType)
                throw new InvalidOperationException("Incorrect message type.");

            return new()
            {
                GameType = (GameTypes)int.Parse(msg.Payload)
            };
        }
    }
}
