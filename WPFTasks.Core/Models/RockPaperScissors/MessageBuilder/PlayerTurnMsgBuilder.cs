using TopNetwork.Core;
using TopNetwork.Services.MessageBuilder;

namespace WPFTasks.Core.Models.RockPaperScissors.MessageBuilder
{
    public class PlayerTurnData : IMsgSourceData
    {
        public string Prompt { get; set; } = "Ваш ход. Введите R, P или S.";
        public string MessageType => MsgType;
        public static string MsgType => "RpsPlayerTurnNotification";
    }

    public class PlayerTurnMsgBuilder : IMessageBuilder<PlayerTurnData>
    {
        private PlayerTurnData _data = new();

        public PlayerTurnMsgBuilder SetPrompt(string prompt)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(prompt, nameof(prompt));
            _data.Prompt = prompt;
            return this;
        }

        public Message BuildMsg()
        {
            return new Message
            {
                MessageType = PlayerTurnData.MsgType,
                Payload = _data.Prompt
            };
        }

        public static PlayerTurnData Parse(Message msg)
        {
            if (msg.MessageType != PlayerTurnData.MsgType)
                throw new InvalidOperationException("Incorrect message type.");

            return new PlayerTurnData
            {
                Prompt = msg.Payload
            };
        }
    }
}
