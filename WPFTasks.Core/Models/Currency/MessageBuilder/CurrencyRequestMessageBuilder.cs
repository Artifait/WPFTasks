
using TopNetwork.Core;
using TopNetwork.Services.MessageBuilder;

namespace WPFTasks.Core.Models.Currency.MessageBuilder
{
    public class CurrencyRequestData : IMsgSourceData
    {
        public string FromCurrency { get; set; } = string.Empty;
        public string ToCurrency { get; set; } = string.Empty;

        public string MessageType => "CurrencyRequest";
    }

    public class CurrencyRequestMessageBuilder : IMessageBuilder<CurrencyRequestData>
    {
        private CurrencyRequestData _data = new();

        public Message BuildMsg()
        {
            return new Message()
            {
                MessageType = _data.MessageType,
                Payload = $"{_data.FromCurrency}:{_data.ToCurrency}"
            };
        }

        public CurrencyRequestData Parse(Message msg)
        {
            if (msg.MessageType != _data.MessageType)
                throw new InvalidOperationException("Incorrect message type.");

            string[] words = msg.Payload.Split(':');
            if(words.Length == 2)
            {
                return new CurrencyRequestData()
                {
                    FromCurrency = words[0],
                    ToCurrency = words[1]
                };
            }

            throw new ArgumentException($"Не валидный Payload: {msg.Payload}...");
        }
    }
}
