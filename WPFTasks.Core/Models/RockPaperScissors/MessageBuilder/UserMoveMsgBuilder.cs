using TopNetwork.Core;
using TopNetwork.Services.MessageBuilder;

namespace WPFTasks.Core.Models.RockPaperScissors.MessageBuilder
{
    public class UserMoveData : IMsgSourceData
    {
        /// <summary>
        ///     Значения: 0 - Rock, 1 - Paper, 2 - Scissors.
        /// </summary>
        public int Move { get; set; }
        public string MessageType => MsgType;
        public static string MsgType => "RpsUserMove";
    }

    public class UserMoveMsgBuilder : IMessageBuilder<UserMoveData>
    {
        private UserMoveData _data = new();

        /// <summary>
        ///     Устанавливает ход: 0 - Rock, 1 - Paper, 2 - Scissors.
        /// </summary>
        public UserMoveMsgBuilder SetMove(int move)
        {
            if (move < 0 || move > 2)
                throw new ArgumentException("Недопустимый ход. Допустимые значения: 0 (Rock), 1 (Paper), 2 (Scissors).");

            _data.Move = move;
            return this;
        }
        public Message BuildMsg()
        {
            return new Message
            {
                MessageType = _data.MessageType,
                Payload = _data.Move.ToString()
            };
        }

        public static UserMoveData Parse(Message msg)
        {
            if (msg.MessageType != UserMoveData.MsgType)
                throw new InvalidOperationException("Incorrect message type.");

            return new UserMoveData
            {
                Move = int.Parse(msg.Payload)
            };
        }
    }
}
