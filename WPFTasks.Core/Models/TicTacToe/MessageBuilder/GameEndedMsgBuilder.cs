
using TopNetwork.Core;
using TopNetwork.Services.MessageBuilder;

namespace WPFTasks.Core.Models.TicTacToe.MessageBuilder
{
    public class GameEndedData : IMsgSourceData
    {
        public string GameStatus {  get; set; } = string.Empty;
        public char[,] Board { get; set; } = new char[3,3]; 

        public string MessageType => MsgType;
        public static string MsgType => "GameEndedNotification";
    }

    public class GameEndedMsgBuilder : IMessageBuilder<GameEndedData>
    {
        private GameEndedData _data = new();

        public GameEndedMsgBuilder SetBoard(char[,] board)
        {
            if (board.GetLength(0) != 3 || board.GetLength(1) != 3)
                throw new ArgumentException("Массив должен быть размером 3x3");

            _data.Board = board;
            return this;
        }

        public GameEndedMsgBuilder SetGameStatus(string status)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(status, nameof(status));
            _data.GameStatus = status;

            return this;
        }

        public Message BuildMsg()
        {
            return new()
            {
                MessageType = _data.MessageType,
                Headers =
                {
                    { nameof(GameEndedData.Board), UpdateGameBoardMsgBuilder.ArrayToString(_data.Board) }
                },
                Payload = _data.GameStatus
            };
        }

        public static GameEndedData Parse(Message msg)
        {
            if (msg.MessageType != GameEndedData.MsgType)
                throw new InvalidOperationException("Incorrect message type.");

            return new()
            {
                GameStatus = msg.Payload,
                Board = UpdateGameBoardMsgBuilder.StringToArray(msg.Headers[nameof(GameEndedData.Board)])
            };
        }
    }
}
