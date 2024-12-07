
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace TopNetwork.Core
{
    public class TopClient
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private readonly ConcurrentQueue<Message> _messageQueue = new();
        private readonly object _eventLock = new();
        private Action<Message>? _onAcceptedMessage;
        private readonly SemaphoreSlim _streamSemaphore = new(1, 1); // Индивидуальный семафор для потоков клиента

        public event Action? OnDisconnected;
        public TcpClient Client => _client;
        public NetworkStream Stream => _stream;

        public EndPoint? RemoteEndPoint => _client.Client.RemoteEndPoint;
        public SemaphoreSlim StreamSemaphore => _streamSemaphore; // Доступ к семафору клиента
        public Action<Message>? OnAcceptedMessage
        {
            get => _onAcceptedMessage;
            set
            {
                lock (_eventLock)
                {
                    _onAcceptedMessage = value;

                    // Если подписка появилась, обрабатываем сообщения из очереди
                    if (_onAcceptedMessage != null)
                    {
                        _ = ProcessQueuedMessages();
                    }
                }
            }
        }

        public TopClient(string ip, int port)
        {
            _client = new TcpClient(ip, port);
            _stream = _client.GetStream();
        }

        public TopClient(TcpClient client)
        {
            _client = client;
            _stream = _client.GetStream();
        }

        public async Task SendMessageAsync(Message msg)
        {
            await DeliveryService.SendMessageAsync(_stream, msg);
        }
        public void Close() => Disconnect();
        public async Task StartListen(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        var msg = await DeliveryService.AcceptMessageAsync(this, token);
                        EnqueueOrInvoke(msg);
                    }
                    catch (OperationCanceledException)
                    {
                        // Завершаем прослушивание при отмене
                        break;
                    }
                    catch (IOException)
                    {
                        // Поток завершён, инициируем отключение
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка при получении сообщения: {ex.Message}");
                        break;
                    }
                }
            }
            finally
            {
                Disconnect();
            }
        }
        public void Disconnect()
        {
            if (_client.Connected)
            {
                try
                {
                    _stream.Close();
                    _client.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при закрытии клиента: {ex.Message}");
                }
            }
            _streamSemaphore.Dispose(); // Уничтожение семафора при отключении клиента
            // Вызываем событие OnDisconnected
            OnDisconnected?.Invoke();
        }

        private void EnqueueOrInvoke(Message msg)
        {
            lock (_eventLock)
            {
                if (_onAcceptedMessage == null)
                {
                    // Если нет подписчиков, добавляем сообщение в очередь
                    _messageQueue.Enqueue(msg);
                }
                else
                {
                    // Если есть подписчики, проверяем статус клиента
                    if (_client.Connected)
                    {
                        _ = Task.Run(() => _onAcceptedMessage?.Invoke(msg));
                    }
                }
            }
        }
        private async Task ProcessQueuedMessages()
        {
            while (_messageQueue.TryDequeue(out var msg))
            {
                await Task.Run(() => _onAcceptedMessage?.Invoke(msg));
            }
        }
    }
}
