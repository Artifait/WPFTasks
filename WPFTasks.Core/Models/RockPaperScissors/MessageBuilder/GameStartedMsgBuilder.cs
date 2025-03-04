using TopNetwork.Core;
using TopNetwork.Services.MessageBuilder;

namespace WPFTasks.Core.Models.RockPaperScissors.MessageBuilder
{
    public class GameStartedData : IMsgSourceData
    {
        public string Instructions { get; set; } = "Выберите ход: R - Камень, P - Бумага, S - Ножницы";
        public string MessageType => MsgType;
        public static string MsgType => "RpsGameStartedNotification";
    }

    public class GameStartedMsgBuilder : IMessageBuilder<GameStartedData>
    {
        private GameStartedData _data = new();

        public GameStartedMsgBuilder SetInstructions(string instructions)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(instructions, nameof(instructions));
            _data.Instructions = instructions;
            return this;
        }

        public Message BuildMsg()
        {
            return new Message
            {
                MessageType = _data.MessageType,
                Payload = _data.Instructions
            };
        }

        public static GameStartedData Parse(Message msg)
        {
            if (msg.MessageType != GameStartedData.MsgType)
                throw new InvalidOperationException("Incorrect message type.");

            return new GameStartedData
            {
                Instructions = msg.Payload
            };
        }
    }
}
