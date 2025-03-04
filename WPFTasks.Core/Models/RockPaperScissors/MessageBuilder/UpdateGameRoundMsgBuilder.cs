using TopNetwork.Core;
using TopNetwork.Services.MessageBuilder;

namespace WPFTasks.Core.Models.RockPaperScissors.MessageBuilder
{
    public class UpdateGameRoundData : IMsgSourceData
    {
        public char Player1Move { get; set; }
        public char Player2Move { get; set; }
        public string MessageType => MsgType;
        public static string MsgType => "RpsUpdateRound";
    }

    public class UpdateGameRoundMsgBuilder : IMessageBuilder<UpdateGameRoundData>
    {
        private UpdateGameRoundData _data = new();

        public UpdateGameRoundMsgBuilder SetPlayerMoves(char player1Move, char player2Move)
        {
            _data.Player1Move = player1Move;
            _data.Player2Move = player2Move;
            return this;
        }

        public Message BuildMsg()
        {
            return new Message
            {
                MessageType = _data.MessageType,
                Headers =
                {
                    { nameof(UpdateGameRoundData.Player1Move), $"{_data.Player1Move}" },
                    { nameof(UpdateGameRoundData.Player2Move), $"{_data.Player2Move}" }
                },
                Payload = $"{_data.Player1Move}-{_data.Player2Move}"
            };
        }

        public static UpdateGameRoundData Parse(Message msg)
        {
            if (msg.MessageType != UpdateGameRoundData.MsgType)
                throw new InvalidOperationException("Incorrect message type.");

            return new UpdateGameRoundData
            {
                Player1Move = msg.Headers[nameof(UpdateGameRoundData.Player1Move)][0],
                Player2Move = msg.Headers[nameof(UpdateGameRoundData.Player2Move)][0]
            };
        }
    }
}
