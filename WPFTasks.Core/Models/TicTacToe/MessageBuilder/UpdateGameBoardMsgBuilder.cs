
using System.Text;
using TopNetwork.Core;
using TopNetwork.Services.MessageBuilder;

namespace WPFTasks.Core.Models.TicTacToe.MessageBuilder
{
    public class UpdateGameBoardData : IMsgSourceData
    {
        public char TurnSymbol { get; set; }
        public char[,] Board { get; set; } = new char[3, 3];

        public string MessageType => MsgType;
        public static string MsgType => "UpdateGameBoard";
    }

    public class UpdateGameBoardMsgBuilder : IMessageBuilder<UpdateGameBoardData>
    {
        private UpdateGameBoardData _data = new();

        public UpdateGameBoardMsgBuilder SetBoard(char[,] board)
        {
            if (board.GetLength(0) != 3 || board.GetLength(1) != 3)
                throw new ArgumentException("Массив должен быть размером 3x3");

            _data.Board = board;
            return this;
        }

        public UpdateGameBoardMsgBuilder SetTurnSymbol(char turnSymbol)
        {
            _data.TurnSymbol = turnSymbol;
            return this;
        }

        public Message BuildMsg()
        {
            return new()
            {
                MessageType = _data.MessageType,
                Headers =
                {
                    { nameof(UpdateGameBoardData.TurnSymbol), $"{_data.TurnSymbol}" }
                },
                Payload = ArrayToString(_data.Board)
            };
        }

        public static UpdateGameBoardData Parse(Message msg)
        {
            if (msg.MessageType != UpdateGameBoardData.MsgType)
                throw new InvalidOperationException("Incorrect message type.");

            return new()
            {
                Board = StringToArray(msg.Payload),
                TurnSymbol = msg.Headers[nameof(UpdateGameBoardData.TurnSymbol)][0]
            };
        }

        public static string ArrayToString(char[,] array)
        {
            if (array.GetLength(0) != 3 || array.GetLength(1) != 3)
                throw new ArgumentException("Массив должен быть размером 3x3");

            StringBuilder result = new();
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    result.Append(array[i, j]);
                }
            }
            return result.ToString();
        }

        public static char[,] StringToArray(string input)
        {
            if (input.Length != 9)
                throw new ArgumentException("Строка должна содержать ровно 9 символов");

            char[,] array = new char[3, 3];
            for (int i = 0; i < 9; i++)
            {
                array[i / 3, i % 3] = input[i];
            }
            return array;
        }

    }
}
