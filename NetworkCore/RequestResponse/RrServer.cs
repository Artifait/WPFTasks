
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using TopNetwork.Core;

namespace TopNetwork.RequestResponse
{
    public delegate void LogString(string message);
    /// <summary>
    /// Rr -> Request Response Server
    /// </summary>
    public class RrServer
    {
        private readonly TcpListener _listener;
        private readonly ConcurrentDictionary<Guid, ClientSession> _sessions = new();
        private readonly ServiceRegistry _serviceRegistry = new();
        private readonly Func<TopClient, ServiceRegistry, ClientSession> _sessionFactory;
        private CancellationTokenSource? _cancellationTokenSource;

        // События
        public event Action<TopClient>? ClientConnected;
        public event Action<TopClient>? ClientDisconnected;
        public event Action<Exception>? ServerError;

        public LogString? Logger { get; set; }

        public RrServer(IPEndPoint endPoint, Func<TopClient, ServiceRegistry, ClientSession> sessionFactory)
        {
            _listener = new TcpListener(endPoint ?? throw new ArgumentNullException(nameof(endPoint)));
            _sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
        }

        /// <summary> Регистрирует сервис для использования сессиями. </summary>
        public void RegisterService<TService>(TService service) where TService : class
            => _serviceRegistry.Register(service);

        /// <summary> Получает зарегистрированный сервис. </summary>
        public TService? GetService<TService>() where TService : class
            => _serviceRegistry.Get<TService>();

        /// <summary> Запускает сервер для обработки подключений. </summary>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (_cancellationTokenSource != null)
                throw new InvalidOperationException("Server is already running.");

            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _listener.Start();
            Logger?.Invoke("Server started.");

            try
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    var tcpClient = await _listener.AcceptTcpClientAsync();
                    _ = Task.Run(() => HandleNewClientAsync(tcpClient), _cancellationTokenSource.Token);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                ServerError?.Invoke(ex);
            }
            finally
            {
                StopListening();
                Logger?.Invoke("Server stopped.");
            }
        }

        /// <summary> Останавливает сервер и закрывает все сессии. </summary>
        public async Task StopAsync()
        {
            if (_cancellationTokenSource == null)
                throw new InvalidOperationException("Server is not running.");

            _cancellationTokenSource.Cancel();
            StopListening();

            foreach (var session in _sessions.Values)
            {
                session.CloseSession();
            }
            _sessions.Clear();

            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }

        private void StopListening()
        {
            try
            {
                _listener.Stop();
            }
            catch (Exception ex)
            {
                Logger?.Invoke($"Error while stopping the listener: {ex.Message}");
            }
        }

        private async Task HandleNewClientAsync(TcpClient tcpClient)
        {
            var clientGuid = Guid.NewGuid();
            TopClient? topClient = null;
            ClientSession? session = null;

            try
            {
                topClient = new TopClient();
                topClient.Connect(tcpClient);

                session = _sessionFactory(topClient, _serviceRegistry);

                if (!_sessions.TryAdd(clientGuid, session))
                {
                    throw new InvalidOperationException("Failed to add client session.");
                }

                ClientConnected?.Invoke(topClient);
                Logger?.Invoke($"[{topClient.RemoteEndPoint}]: Client Connected...");

                await session.StartAsync();
            }
            catch (Exception ex)
            {
                ServerError?.Invoke(ex);
                Logger?.Invoke($"[{topClient!.RemoteEndPoint}]: Error handling client - {ex.Message}");
            }
            finally
            {
                if (session != null)
                {
                    _sessions.TryRemove(clientGuid, out _);
                    session.CloseSession();
                }

                ClientDisconnected?.Invoke(topClient!);
                Logger?.Invoke($"[{topClient?.RemoteEndPoint}]: Client disconnected...");
            }
        }
    }
}
