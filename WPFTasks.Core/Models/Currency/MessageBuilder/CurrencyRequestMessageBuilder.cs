
using TopNetwork.Core;
using TopNetwork.Services.MessageBuilder;

namespace WPFTasks.Core.Models.Currency.MessageBuilder
{
    public class CurrencyRequestData : IMsgSourceData
    {
        public string FromCurrency { get; set; } = string.Empty;
        public string ToCurrency { get; set; } = string.Empty;

        public string MessageType => MsgType;
        public static string MsgType => "CurrencyRequest";
    }

    public class CurrencyRequestMessageBuilder : IMessageBuilder<CurrencyRequestData>
    {
        private CurrencyRequestData _data = new();

        public CurrencyRequestMessageBuilder SetFromCurrency(string fromCurrency)
        {
            _data.FromCurrency = fromCurrency;
            return this;
        }

        public CurrencyRequestMessageBuilder SetToCurrency(string toCurrency)
        {
            _data.ToCurrency = toCurrency;
            return this;
        }

        public Message BuildMsg()
        {
            return new Message()
            {
                MessageType = _data.MessageType,
                Payload = $"{_data.FromCurrency}:{_data.ToCurrency}"
            };
        }

        public static CurrencyRequestData Parse(Message msg)
        {
            if (msg.MessageType != CurrencyRequestData.MsgType)
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
