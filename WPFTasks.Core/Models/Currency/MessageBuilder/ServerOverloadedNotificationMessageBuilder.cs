
using TopNetwork.Core;
using TopNetwork.Services.MessageBuilder;

namespace WPFTasks.Core.Models.Currency.MessageBuilder
{
    public class ServerOverloadedNotificationData : IMsgSourceData
    {
        public string MessageType => MsgType;
        public static string MsgType => "ServerOverloadedNotification";
    }

    public class ServerOverloadedNotificationMessageBuilder : IMessageBuilder<ServerOverloadedNotificationData>
    {
        public Message BuildMsg()
        {
            return new Message()
            {
                MessageType = ServerOverloadedNotificationData.MsgType
            };
        }
    }
}
