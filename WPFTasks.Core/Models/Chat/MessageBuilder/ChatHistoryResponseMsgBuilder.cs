

using System.Text.Json;
using TopNetwork.Core;
using TopNetwork.Services.MessageBuilder;
using WPFTasks.Core.Models.Chat.Services;

namespace WPFTasks.Core.Models.Chat.MessageBuilder
{

    public class ChatHistoryResponseData : IMsgSourceData
    {
        public List<ChatMessage> Messages { get; set; } = [];

        public string MessageType => MsgType;
        public static string MsgType => "ChatHistoryResponse";
    }

    public class ChatHistoryResponseMsgBuilder : IMessageBuilder<ChatHistoryResponseData> 
    {
        private ChatHistoryResponseData _data = new();

        public ChatHistoryResponseMsgBuilder AddMessages(List<ChatMessage> messages)
        {
            _data.Messages.AddRange(messages);
            return this;
        }

        public ChatHistoryResponseMsgBuilder AddMessage(ChatMessage message)
        {
            _data.Messages.Add(message);
            return this;
        }

        public Message BuildMsg()
        {
            return new()
            {
                Payload = JsonSerializer.Serialize(_data.Messages),
                MessageType = _data.MessageType,
            };
        }

        public static ChatHistoryResponseData Parse(Message msg)
        {
            if (msg.MessageType != ChatHistoryResponseData.MsgType)
                throw new InvalidOperationException("Incorrect message type.");

            return new()
            {
                Messages = JsonSerializer.Deserialize<List<ChatMessage>>(msg.Payload) ?? throw new Exception("Не удалось десериализовать в List<ChatMessage>")
            };
        }
    }
}
