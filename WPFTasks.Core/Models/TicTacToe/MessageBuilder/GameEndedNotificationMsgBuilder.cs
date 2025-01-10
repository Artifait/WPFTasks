
using TopNetwork.Core;
using TopNetwork.Services.MessageBuilder;

namespace WPFTasks.Core.Models.TicTacToe.MessageBuilder
{
    public class GameEndedNotificationData : IMsgSourceData
    {
        public string YourStatus {  get; set; } = string.Empty;
        
        public string MessageType => MsgType;
        public static string MsgType => "GameEndedNotification";
    }

    public class GameEndedNotificationMsgBuilder : IMessageBuilder<GameEndedNotificationData>
    {
        private GameEndedNotificationData _data = new();

        public GameEndedNotificationMsgBuilder SetUserStatus(string status)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(status, nameof(status));
            _data.YourStatus = status;

            return this;
        }

        public Message BuildMsg()
        {
            return new()
            {
                MessageType = _data.MessageType,
                Payload = _data.YourStatus
            };
        }

        public static GameEndedNotificationData Parse(Message msg)
        {
            if (msg.MessageType != GameEndedNotificationData.MsgType)
                throw new InvalidOperationException("Incorrect message type.");

            return new()
            {
                YourStatus = msg.Payload
            };
        }
    }
}
