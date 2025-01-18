
using TopNetwork.Core;
using TopNetwork.Services.MessageBuilder;

namespace WPFTasks.Core.Models.Chat.MessageBuilder
{
    public class ChatHistoryRequestData : IMsgSourceData
    {
        public string ChatId { get; set; } = string.Empty;

        public string MessageType => MsgType;
        public static string MsgType => "ChatHistoryRequest";
    }

    public class ChatHistoryRequestMsgBuilder : IMessageBuilder<ChatHistoryRequestData>
    {
        private ChatHistoryRequestData _data = new();

        public ChatHistoryRequestMsgBuilder SetChat(string id)
        {
            _data.ChatId = id;
            return this;
        }


        public Message BuildMsg()
        {
            return new()
            {
                Payload = _data.ChatId,
                MessageType = _data.MessageType,
            };
        }

        public static ChatHistoryRequestData Parse(Message msg)
        {
            if (msg.MessageType != ChatHistoryRequestData.MsgType)
                throw new InvalidOperationException("Incorrect message type.");

            return new()
            {
                ChatId = msg.Payload
            };
        }
    }
}
