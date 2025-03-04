

using TopNetwork.Core;
using TopNetwork.Services.MessageBuilder;
using WPFTasks.Core.Models.RockPaperScissors.Services.RockPaperScissorsLogic;

namespace WPFTasks.Core.Models.RockPaperScissors.MessageBuilder
{
    public class FindGameRequestData : IMsgSourceData
    {
        public GameTypes GameType { get; set; }
        public string MessageType => MsgType;
        public static string MsgType => "RpsFindGameRequest";
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
            return new Message
            {
                MessageType = _data.MessageType,
                Payload = ((int)_data.GameType).ToString(),
            };
        }

        public static FindGameRequestData Parse(Message msg)
        {
            if (msg.MessageType != FindGameRequestData.MsgType)
                throw new InvalidOperationException("Incorrect message type.");

            return new FindGameRequestData
            {
                GameType = (GameTypes)int.Parse(msg.Payload)
            };
        }
    }
}
