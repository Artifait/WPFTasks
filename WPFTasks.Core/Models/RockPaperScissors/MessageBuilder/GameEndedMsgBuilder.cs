using TopNetwork.Core;
using TopNetwork.Services.MessageBuilder;

namespace WPFTasks.Core.Models.RockPaperScissors.MessageBuilder
{
    public class GameEndedData : IMsgSourceData
    {
        public string GameResult { get; set; } = string.Empty; // Например: "Ничья" или "Player1 выигрывает"
        public char Player1Move { get; set; }
        public char Player2Move { get; set; }
        public string MessageType => MsgType;
        public static string MsgType => "RpsGameEndedNotification";
    }

    public class GameEndedMsgBuilder : IMessageBuilder<GameEndedData>
    {
        private GameEndedData _data = new();

        public GameEndedMsgBuilder SetPlayerMoves(char player1Move, char player2Move)
        {
            _data.Player1Move = player1Move;
            _data.Player2Move = player2Move;
            return this;
        }

        public GameEndedMsgBuilder SetGameResult(string result)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(result, nameof(result));
            _data.GameResult = result;
            return this;
        }

        public Message BuildMsg()
        {
            return new Message
            {
                MessageType = _data.MessageType,
                Headers =
                {
                    { nameof(GameEndedData.Player1Move), $"{_data.Player1Move}" },
                    { nameof(GameEndedData.Player2Move), $"{_data.Player2Move}" }
                },
                Payload = _data.GameResult
            };
        }

        public static GameEndedData Parse(Message msg)
        {
            if (msg.MessageType != GameEndedData.MsgType)
                throw new InvalidOperationException("Incorrect message type.");

            return new GameEndedData
            {
                GameResult = msg.Payload,
                Player1Move = msg.Headers[nameof(GameEndedData.Player1Move)][0],
                Player2Move = msg.Headers[nameof(GameEndedData.Player2Move)][0]
            };
        }
    }
}
