
using TopNetwork.Core;
using TopNetwork.Services.MessageBuilder;

namespace WPFTasks.Core.Models.TicTacToe.MessageBuilder
{
    public class GameEndedData : IMsgSourceData
    {
        public string GameStatus {  get; set; } = string.Empty;
        
        public string MessageType => MsgType;
        public static string MsgType => "GameEndedNotification";
    }

    public class GameEndedMsgBuilder : IMessageBuilder<GameEndedData>
    {
        private GameEndedData _data = new();

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
                Payload = _data.GameStatus
            };
        }

        public static GameEndedData Parse(Message msg)
        {
            if (msg.MessageType != GameEndedData.MsgType)
                throw new InvalidOperationException("Incorrect message type.");

            return new()
            {
                GameStatus = msg.Payload
            };
        }
    }
}
