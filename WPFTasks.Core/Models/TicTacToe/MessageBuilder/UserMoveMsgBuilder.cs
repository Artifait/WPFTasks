
using TopNetwork.Core;
using TopNetwork.Services.MessageBuilder;

namespace WPFTasks.Core.Models.TicTacToe.MessageBuilder
{
    public class UserMoveData : IMsgSourceData
    {
        /// <summary>
        ///     TicTacToe Data Position<br/>
        ///         0|1|2<br/>
        ///         3|4|5<br/>
        ///         6|7|8 
        /// </summary>
        public int CellNumber;
        public string MessageType => MsgType;
        public static string MsgType => "UserMove";
    }

    public class UserMoveMsgBuilder : IMessageBuilder<UserMoveData>
    {
        private UserMoveData _data = new();

        /// <summary>
        ///     TicTacToe Data Position<br/>
        ///         0|1|2<br/>
        ///         3|4|5<br/>
        ///         6|7|8 
        /// </summary>
        public UserMoveMsgBuilder SetCellNumber(int cellNumber)
        {
            if (cellNumber < 0 || cellNumber > 8)
                throw new ArgumentException("Не валидный номер ячейки");

            _data.CellNumber = cellNumber;
            return this;
        }
        public Message BuildMsg()
        {
            return new()
            {
                MessageType = _data.MessageType,
                Payload = _data.CellNumber.ToString()
            };
        }

        public static UserMoveData Parse(Message msg)
        {
            if (msg.MessageType != UserMoveData.MsgType)
                throw new InvalidOperationException("Incorrect message type.");

            return new()
            {
                CellNumber = int.Parse(msg.Payload)
            };
        }
    }
}
