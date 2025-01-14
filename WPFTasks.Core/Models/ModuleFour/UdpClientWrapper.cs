
using System.Net.Sockets;
using System.Net;
using TopNetwork.Core;

namespace WPFTasks.Core.Models.ModuleFour
{
    public class UdpClientWrapper
    {
        private readonly UdpClient _udpClient;
        private readonly IPEndPoint _serverEndpoint;
        private readonly CancellationTokenSource _cts = new();


        public event Action<Message>? OnMessageReceived;
        public event Action<string>? OnErrore;
        public bool IsStarted { get; private set; } = false;

        public UdpClientWrapper(IPEndPoint serverEndpoint, int localPort)
        {
            _serverEndpoint = serverEndpoint;
            _udpClient = new UdpClient(localPort);
        }

        public async Task<bool> SubscribeToMessageTypeAsync(string messageType)
        {
            var subscriptionMessage = new Message
            {
                MessageType = "Subscribe",
                Headers = new Dictionary<string, string>
                {
                    { "MessageType", messageType }
                }
            };

            return await SendMessageAsync(subscriptionMessage);
        }

        public async Task<bool> SendMessageAsync(Message message)
        {
            byte[] data = message.ToBytes();

            try
            {
                await _udpClient.SendAsync(data, data.Length, _serverEndpoint);
                return true;
            }
            catch (Exception ex)
            {
                OnErrore?.Invoke($"Ошибка отправки сообщения: {ex.Message}");
                return false;
            }
        }

        public async Task StartListeningAsync()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    var result = await _udpClient.ReceiveAsync().WaitAsync(_cts.Token);
                    var message = Message.FromBytes(result.Buffer);

                    OnMessageReceived?.Invoke(message);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    OnErrore?.Invoke($"Ошибка обработки сообщения: {ex.Message}");
                }
            }
        }

        public void StopListening()
        {
            _cts.Cancel();
            _udpClient.Close();
        }
    }
}
