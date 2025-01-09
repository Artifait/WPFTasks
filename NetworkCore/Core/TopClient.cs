
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace TopNetwork.Core
{
    public class TopClient : IEquatable<TopClient>
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private readonly SemaphoreSlim _streamSemaphore = new(1, 1);
        private readonly object _stateLock = new();
        private readonly ConcurrentQueue<Message> _messageQueue = new();

        private CancellationTokenSource? _listeningCancellationTokenSource;

        // Events
        public event Func<Message, Task>? OnMessageReceived;
        public event Action? OnConnected;
        public event Action? OnDisconnected;
        public event Action? OnConnectionLost;
        public event Action? OnListeningStarted;
        public event Action? OnListeningStopped;
        public event Action<Exception>? OnError;

        // Properties
        public bool IsConnected => _client?.Connected ?? false;
        public bool IsInitialized { get; private set; }
        public EndPoint? RemoteEndPoint => _client?.Client?.RemoteEndPoint;
        public EndPoint? LastUseEndPoint {  get; private set; }
        public NetworkStream? ReadStream => _stream;
        public readonly SemaphoreSlim ReadSemaphore = new(1, 1);

        public bool HasPendingMessages => !_messageQueue.IsEmpty;

        // Public Methods
        public async Task<TopClient> ConnectAsync(string ip, int port)
        {
            _client = new TcpClient();
            await _client.ConnectAsync(ip, port);
            _stream = _client.GetStream();
            IsInitialized = true;

            OnConnected?.Invoke();

            return this;
        }

        public TopClient Connect(TcpClient client)
        {
            if (IsInitialized)
                throw new InvalidOperationException("Client is already initialized.");

            _client = client;
            _stream = client.GetStream();
            IsInitialized = true;

            OnConnected?.Invoke();
            LastUseEndPoint = _client.Client.RemoteEndPoint;
            return this;
        }

        public TopClient Connect(string ip, int port)
        {
            _client = new TcpClient();
            _client.Connect(ip, port);
            _stream = _client.GetStream();
            IsInitialized = true;

            OnConnected?.Invoke();
            LastUseEndPoint = _client.Client.RemoteEndPoint;
            return this;
        }

        public void Disconnect()
        {
            lock (_stateLock)
            {
                if (!IsConnected)
                    return;

                try
                {
                    _listeningCancellationTokenSource?.Cancel();
                    _stream?.Close();
                    _client?.Close();
                }
                finally
                {
                    IsInitialized = false;
                    OnDisconnected?.Invoke();
                }
            }
        }

        public async Task SendMessageAsync(Message message, CancellationToken cancellationToken = default)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Client is not initialized.");

            if (_stream == null)
                throw new InvalidOperationException("Stream is not available.");

            await _streamSemaphore.WaitAsync(cancellationToken);
            try
            {
                await DeliveryService.SendMessageAsync(_stream, message);
            }
            finally
            {
                _streamSemaphore.Release();
            }
        }

        public async Task StartListeningAsync(CancellationToken cancellationToken = default)
        {
            if (!IsInitialized)
                throw new InvalidOperationException("Client is not initialized.");

            _listeningCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            OnListeningStarted?.Invoke();

            try
            {
                while (!_listeningCancellationTokenSource.Token.IsCancellationRequested && IsConnected)
                {
                    try
                    {
                        var message = await DeliveryService.AcceptMessageAsync(this, _listeningCancellationTokenSource.Token);
                        _messageQueue.Enqueue(message);

                        if (OnMessageReceived != null)
                            await OnMessageReceived.Invoke(message);
                    }
                    catch (Exception ex)
                    {
                        OnError?.Invoke(ex);
                        break;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal operation on cancellation
            }
            finally
            {
                StopListening();
                _client?.Close();
                OnConnectionLost?.Invoke();
            }
        }

        public void StopListening()
        {
            _listeningCancellationTokenSource?.Cancel();
            OnListeningStopped?.Invoke();
        }

        public bool TryDequeueMessage(out Message? message)
        {
            return _messageQueue.TryDequeue(out message);
        }

        // Equality Members
        public override int GetHashCode()
            => LastUseEndPoint?.ToString()?.GetHashCode() ?? 0;

        public override bool Equals(object? obj)
            => Equals(obj as TopClient);

        public bool Equals(TopClient? other) =>
            other != null &&
            IsInitialized == other.IsInitialized &&
            LastUseEndPoint?.Equals(other.LastUseEndPoint) == true;

        public static bool operator ==(TopClient? left, TopClient? right)
            => ReferenceEquals(left, right) || (left?.Equals(right) ?? false);

        public static bool operator !=(TopClient? left, TopClient? right)
            => !(left == right);
    }
}
