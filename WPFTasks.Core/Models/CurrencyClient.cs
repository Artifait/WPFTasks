
using TopNetwork.Core;

namespace WPFTasks.Core.Models
{
    using MsgT = CurrencyServer.MessageType;

    public class CurrencyClient
    {
        public bool Authenticated { get; set; } = false;

        private readonly TopClient _client;
        private readonly CancellationTokenSource _cts;

        public event Action<string> OnGetCurrencyRateMsg;
        public event Action<string> OnGetErrorMsg;

        public CurrencyClient(string serverIp, int port, string login, string password)
        {
            _client = new TopClient(serverIp, port);
            _cts = new CancellationTokenSource();

            _client.OnAcceptedMessage += OnMessageFromServer;
            _ = _client.StartListen(_cts.Token);

            Authentication(login, password);
        }
        private async Task Authentication(string login, string password)
        {
            Message request = new Message
            {
                MessageType = CurrencyServer.GetMessageTypeStr(MsgT.Authentication),
                Payload = $"{login} {password}"
            };

            await _client.SendMessageAsync(request);
        }
        public async Task RequestExchangeRate(string fromCurrency, string toCurrency)
        {
            Message request = new Message
            {
                MessageType = CurrencyServer.GetMessageTypeStr(MsgT.CurrencyConversion),
                Payload = $"{fromCurrency} {toCurrency}"
            };

            await _client.SendMessageAsync(request);
        }

        private static void OnMessageFromServer(Message msg)
        {
            if(msg.MessageType == CurrencyServer.GetMessageTypeStr(MsgT.Authentication))
            {

            }
            else if (msg.MessageType == CurrencyServer.GetMessageTypeStr(MsgT.CurrencyRate))
            {
                Console.WriteLine($"Курс {msg.Headers["FromCurrency"]} -> {msg.Headers["ToCurrency"]}: {msg.Payload}");
            }
            else if (msg.MessageType == CurrencyServer.GetMessageTypeStr(MsgT.Error))
            {
                Console.WriteLine($"Ошибка: {msg.Payload}");
            }
            else
            {
                Console.WriteLine("Неизвестный тип сообщения.");
            }
        }
    }
}
