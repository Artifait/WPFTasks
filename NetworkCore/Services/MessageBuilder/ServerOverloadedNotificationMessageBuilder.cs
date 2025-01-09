
using TopNetwork.Core;

namespace TopNetwork.Services.MessageBuilder
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
