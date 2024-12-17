using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace TopNetwork.Core
{
    public class TopClient : IEquatable<TopClient>
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private readonly ConcurrentQueue<Message> _messageQueue = new();
        private readonly object _eventLock = new();
        private readonly SemaphoreSlim _streamSemaphore = new(1, 1);

        private Action<Message>? _onAcceptedMessage;

        public event Action? OnDisconnected;

        public TcpClient Client => _client;
        public NetworkStream Stream => _stream;
        public SemaphoreSlim StreamSemaphore => _streamSemaphore;

        public EndPoint? RemoteEndPoint => _client?.Client.RemoteEndPoint;
        public bool IsConnected => _client?.Connected ?? false;
        public bool IsInitialized { get; private set; } = false;

        public Action<Message>? OnAcceptedMessage
        {
            get => _onAcceptedMessage;
            set
            {
                lock (_eventLock)
                {
                    _onAcceptedMessage = value;
                    if (_onAcceptedMessage != null)
                    {
                        _ = ProcessQueuedMessages();
                    }
                }
            }
        }

        // Пустой клиент (пустышка)
        public TopClient()
        {
            _client = null!;
            _stream = null!;
        }

        // Конструктор с параметрами
        public TopClient(string ip, int port)
        {
            Initialize(ip, port);
        }

        // Конструктор с существующим TcpClient
        public TopClient(TcpClient client)
        {
            _client = client;
            _stream = client.GetStream();
            IsInitialized = true;
        }

        // Метод для инициализации клиента
        public void Initialize(string ip, int port)
        {
            if (IsInitialized)
                throw new InvalidOperationException("Клиент уже инициализирован.");

            _client = new TcpClient(ip, port);
            _stream = _client.GetStream();
            IsInitialized = true;
        }

        public async Task SendMessageAsync(Message msg)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Клиент не инициализирован.");

            try
            {
                await DeliveryService.SendMessageAsync(_stream, msg);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при отправке сообщения: {ex.Message}");
            }
        }

        public void Close() => Disconnect();

        public async Task StartListen(CancellationToken token)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Клиент не инициализирован.");

            try
            {
                while (!token.IsCancellationRequested && IsConnected)
                {
                    try
                    {
                        var msg = await DeliveryService.AcceptMessageAsync(this, token);
                        EnqueueOrInvoke(msg);
                    }
                    catch (OperationCanceledException)
                    {
                        break; // Завершаем по отмене
                    }
                    catch (IOException)
                    {
                        break; // Отключение клиента
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
            lock (_eventLock)
            {
                if (IsConnected)
                {
                    try
                    {
                        _stream?.Close();
                        _client?.Close();
                        _stream?.Dispose();
                        _client?.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка при отключении клиента: {ex.Message}");
                    }
                    finally
                    {
                        OnDisconnected?.Invoke();
                        _streamSemaphore.Dispose();
                        IsInitialized = false;
                    }
                }
            }
        }

        private void EnqueueOrInvoke(Message msg)
        {
            lock (_eventLock)
            {
                if (_onAcceptedMessage == null)
                {
                    _messageQueue.Enqueue(msg);
                }
                else
                {
                    if (IsConnected)
                    {
                        Task.Run(() => _onAcceptedMessage?.Invoke(msg));
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

        #region EqualsZone

        public override int GetHashCode() => RemoteEndPoint?.ToString().GetHashCode() ?? 0;

        public override bool Equals(object? obj) => Equals(obj as TopClient);

        public bool Equals(TopClient? other)
        {
            if (other == null || !IsInitialized || !other.IsInitialized)
                return false;

            return RemoteEndPoint?.ToString() == other.RemoteEndPoint?.ToString();
        }

        public static bool operator ==(TopClient? left, TopClient? right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left is null || right is null) return false;

            return left.Equals(right);
        }

        public static bool operator !=(TopClient? left, TopClient? right) => !(left == right);

        #endregion
    }
}
