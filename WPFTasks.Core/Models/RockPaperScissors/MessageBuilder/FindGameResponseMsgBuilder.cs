using TopNetwork.Core;
using TopNetwork.Services.MessageBuilder;

namespace WPFTasks.Core.Models.RockPaperScissors.MessageBuilder
{
    public class FindGameResponseData : IMsgSourceData
    {
        public string SearchState { get; set; } = string.Empty;
        public string MessageType => MsgType;
        public static string MsgType => "RpsFindGameResponse";
    }

    public class FindGameResponseMsgBuilder : IMessageBuilder<FindGameResponseData>
    {
        private FindGameResponseData _data = new();

        public FindGameResponseMsgBuilder SetSearchState(string state)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(state, nameof(state));
            _data.SearchState = state;
            return this;
        }

        public Message BuildMsg()
        {
            return new Message
            {
                MessageType = FindGameResponseData.MsgType,
                Payload = _data.SearchState
            };
        }

        public static FindGameResponseData Parse(Message msg)
        {
            if (msg.MessageType != FindGameResponseData.MsgType)
                throw new InvalidOperationException("Incorrect message type.");

            return new FindGameResponseData
            {
                SearchState = msg.Payload
            };
        }
    }
}
