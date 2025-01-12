

using TopNetwork.Core;
using TopNetwork.Services.MessageBuilder;

namespace WPFTasks.Core.Models.TicTacToe.MessageBuilder
{
    public class PlayerTurnData : IMsgSourceData
    {
        public char[,] Board { get; set; } = new char[3, 3];
        public string MessageType => MsgType;
        public static string MsgType => "PlayerTurnNotification";
    }
    public class PlayerTurnMsgBuilder : IMessageBuilder<PlayerTurnData>
    {
        private PlayerTurnData _data = new();

        public PlayerTurnMsgBuilder SetBoard(char[,] board)
        {
            if (board.GetLength(0) != 3 || board.GetLength(1) != 3)
                throw new ArgumentException("Массив должен быть размером 3x3");

            _data.Board = board;
            return this;
        }

        public Message BuildMsg()
        {
            return new()
            {
                MessageType = PlayerTurnData.MsgType,
                Payload = UpdateGameBoardMsgBuilder.ArrayToString(_data.Board)
            };
        }

        public static PlayerTurnData Parse(Message msg)
        {
            if (msg.MessageType != PlayerTurnData.MsgType)
                throw new InvalidOperationException("Incorrect message type.");

            return new PlayerTurnData()
            { 
                Board = UpdateGameBoardMsgBuilder.StringToArray(msg.Payload)
            };
        }
    }
}
