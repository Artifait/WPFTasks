
using TopNetwork.Core;
using TopNetwork.Services.MessageBuilder;

namespace WPFTasks.Core.Models.Chat.MessageBuilder
{
    public class UpdateChatData : IMsgSourceData
    {
        public string Sender {  get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string ChatId { get; set; } = string.Empty;

        public string MessageType => MsgType;
        public static string MsgType => "UpdateChat";
    }

    public class UpdateChatMsgBuilder : IMessageBuilder<UpdateChatData>
    {
        private UpdateChatData _data = new();

        public UpdateChatMsgBuilder SetChatId(string chatId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(chatId);
            _data.ChatId = chatId;

            return this;
        }

        public UpdateChatMsgBuilder SetSender(string sender)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(sender);
            _data.Sender = sender;

            return this;
        }

        public UpdateChatMsgBuilder SetContent(string content)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(content);
            _data.Content = content;

            return this;
        }

        public Message BuildMsg()
        {
            return new()
            {
                Payload = $"{_data.ChatId}:{_data.Sender}:{_data.Content}",
                MessageType = _data.MessageType,
            };
        }

        public static UpdateChatData Parse(Message msg)
        {
            if (msg.MessageType != UpdateChatData.MsgType)
                throw new InvalidOperationException("Incorrect message type.");

            var parts = msg.Payload.Split(':');

            return new()
            {
                ChatId = parts[0],
                Sender = parts[1],
                Content = parts[2],
            };
        }
    }
}
