
using TopNetwork.Core;
using TopNetwork.Services.MessageBuilder;

namespace WPFTasks.Core.Models.Currency.MessageBuilder
{
    public class CurrencyResponseData : IMsgSourceData
    {
        public string Payload { get; set; } = string.Empty;
        public string FromCurrency { get; set; } = string.Empty;
        public string ToCurrency { get; set; } = string.Empty;
        public double? Rate { get; set; } = null;

        public string MessageType => "CurrencyResponse";
    }

    public class CurrencyResponseMessageBuilder : IMessageBuilder<CurrencyResponseData>
    {
        private CurrencyResponseData _data = new();

        public CurrencyResponseMessageBuilder SetPayload(string payload)
        {
            _data.Payload = payload;
            return this;
        }

        public CurrencyResponseMessageBuilder SetFromCurrency(string fromCurrency)
        {
            _data.FromCurrency = fromCurrency;
            return this;
        }

        public CurrencyResponseMessageBuilder SetToCurrency(string toCurrency)
        {
            _data.ToCurrency = toCurrency;
            return this;
        }

        public CurrencyResponseMessageBuilder SetRate(double? rate)
        {
            _data.Rate = rate;
            return this;
        }

        public Message BuildMsg()
        {
            return new Message()
            {
                MessageType = _data.MessageType,
                Headers =
                {
                    { nameof(_data.FromCurrency), _data.FromCurrency },
                    { nameof(_data.ToCurrency), _data.ToCurrency },
                    { nameof(_data.Rate), _data.Rate?.ToString() ?? "-1" },
                },
                Payload = _data.Payload
            };
        }

        public CurrencyResponseData Parse(Message msg)
        {
            if (msg.MessageType != _data.MessageType)
                throw new InvalidOperationException("Incorrect message type.");

            return new CurrencyResponseData()
            {
                Payload = msg.Payload,
                FromCurrency = msg.Headers[nameof(CurrencyResponseData.FromCurrency)],
                ToCurrency = msg.Headers[nameof(CurrencyResponseData.ToCurrency)],
                Rate = double.Parse(msg.Headers[nameof(CurrencyResponseData.Rate)]),
            };
        }
    }
}
