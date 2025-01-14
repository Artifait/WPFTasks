
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Net;
using TopNetwork.Core;
using TopNetwork.RequestResponse;

namespace WPFTasks.Core.Models.ModuleFour
{
    public class UdpServer
    {
        private readonly UdpClient _udpServer;
        private readonly ConcurrentDictionary<string, HashSet<IPEndPoint>> _subscriptions = new();
        private readonly CancellationTokenSource _cts = new();
        private readonly ConcurrentBag<IPEndPoint> _blockedPoints;

        public LogString? Logger { get; set; }
        public event Action<string> OnErrore;
        public event Action<IPEndPoint, string> OnClientSubscribe;
        public event Action<IPEndPoint, string> OnClientUnsubscribe;
        public bool IsStarted { get; set; } = false;

        public UdpServer(int port)
        {
            _udpServer = new UdpClient(port);
        }

        public async Task StartAsync()
        {
            Logger?.Invoke("Сервер запущен.");
            IsStarted = true;
            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    var result = await _udpServer.ReceiveAsync().WaitAsync(_cts.Token);

                    var message = Message.FromBytes(result.Buffer);
                    var clientEndpoint = result.RemoteEndPoint;

                    if (message.MessageType == "Subscribe")
                    {
                        if (message.Headers.TryGetValue("MessageType", out var messageType))
                        {
                            AddSubscription(messageType, clientEndpoint);
                            OnClientSubscribe?.Invoke(clientEndpoint, messageType);
                        }
                    }
                    else if (message.MessageType == "Unsubscribe")
                    {
                        if (message.Headers.TryGetValue("MessageType", out var messageType))
                        {
                            TryRemoveSubscription(messageType, clientEndpoint);
                            OnClientUnsubscribe?.Invoke(clientEndpoint, messageType);
                        }
                    }
                    else
                    {
                        Logger?.Invoke($"Получено сообщение от {clientEndpoint}: {message}");
                    }
                }
                catch (Exception ex)
                {
                    OnErrore?.Invoke($"Ошибка обработки сообщения: {ex.Message}");
                }
            }
        }


        public void Stop()
        {
            _cts.Cancel();
            _udpServer.Close();
            IsStarted = false;
        }

        public async Task SendMessageAsync(Message message)
        {
            byte[] data = message.ToBytes();

            if (message.MessageType == "Emergency")
            {
                var allClients = _subscriptions.Values.SelectMany(x => x).Distinct();

                var tasks = allClients.Select(client => SendToClientAsync(client, data));
                await Task.WhenAll(tasks);

                Logger?.Invoke($"Экстренное сообщение отправлено всем клиентам.");
            }
            else
            {
                if (_subscriptions.TryGetValue(message.MessageType, out var clients))
                {
                    var tasks = clients.Select(client => SendToClientAsync(client, data));
                    await Task.WhenAll(tasks);

                    Logger?.Invoke($"Сообщение типа {message.MessageType} отправлено {clients.Count} клиентам.");
                }
                else
                {
                    Logger?.Invoke($"Нет клиентов, подписанных на сообщения типа {message.MessageType}.");
                }
            }
        }

        private void AddSubscription(string messageType, IPEndPoint clientEndpoint)
        {
            _subscriptions.AddOrUpdate(
                messageType,
                _ => new HashSet<IPEndPoint> { clientEndpoint },
                (_, clients) =>
                {
                    clients.Add(clientEndpoint);
                    return clients;
                });
        }

        private void TryRemoveSubscription(string messageType, IPEndPoint clientEndpoint)
        {
            _subscriptions.TryGetValue(messageType, out var clients);
            clients?.Remove(clientEndpoint);
        }

        public void BlockUser(IPEndPoint clientEndpoint)
        {
            _blockedPoints.Add(clientEndpoint);
            Logger?.Invoke($"Пользователь {clientEndpoint} удалён из всех подписок.");
        }

        private async Task SendToClientAsync(IPEndPoint client, byte[] data)
        {
            try {
                if (_blockedPoints.Contains(client)) return;
                await _udpServer.SendAsync(data, data.Length, client);
            }
            catch (Exception ex) {
                OnErrore?.Invoke($"Ошибка отправки данных клиенту {client}: {ex.Message}");
            }
        }
    }
}
