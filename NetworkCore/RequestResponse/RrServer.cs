
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
        private readonly ConcurrentDictionary<Guid, ClientSession> _sessions = new();
        private Func<TopClient, ServiceRegistry, LogString?, ClientSession?>? _sessionFactory;
        private CancellationTokenSource? _cancellationTokenSource;
        private IPEndPoint? _currentEndPoint;
        private TcpListener? _listener;

        // События
        public event Action<TopClient>? ClientConnected;
        public event Action<TopClient>? ClientDisconnected;
        public event Action<Exception>? ServerError;

        public LogString? Logger { get; set; }
        public ServiceRegistry Context { get; private set; } = new();


        // Сеттеры для зависимостей
        public RrServer SetEndPoint(IPEndPoint endPoint)
        {
            _currentEndPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
            _listener = new TcpListener(_currentEndPoint);

            return this;
        }

        public RrServer SetSessionFactory(Func<TopClient, ServiceRegistry, LogString?, ClientSession?> sessionFactory)
        {
            _sessionFactory = sessionFactory ?? throw new ArgumentNullException(nameof(sessionFactory));
            return this;
        }

        /// <summary> Регистрирует сервис для использования сессиями. </summary>
        public RrServer RegisterService<TService>(TService service) where TService : class
        {
            Context.Register(service);
            return this;
        }
        public RrServer RegisterGeneric(Type serviceType, Type implementationType)
        {
            Context.RegisterGeneric(serviceType, implementationType);
            return this;
        }
        /// <summary> Получает зарегистрированный сервис. </summary>
        public TService? GetService<TService>() where TService : class
            => Context.Get<TService>();

        public RrServer GetService<TService>(out TService? service) where TService : class
        {
            service = Context.Get<TService>();
            return this;
        }

        /// <summary> Запускает сервер для обработки подключений. </summary>
        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (_cancellationTokenSource != null)
                throw new InvalidOperationException("Server is already running.");

            // Проверка зависимостей
            if (_listener == null)
                throw new InvalidOperationException("Endpoint is not initialized. Call SetEndPoint() first.");
            if (_sessionFactory == null)
                throw new InvalidOperationException("SessionFactory is not initialized. Call SetSessionFactory() first.");

            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _listener.Start();
            Logger?.Invoke("[Server]: Started.");

            try
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        var tcpClient = await _listener.AcceptTcpClientAsync();
                        _ = Task.Run(() => HandleNewClientAsync(tcpClient), _cancellationTokenSource.Token);
                    }
                    catch (Exception ex)
                    {
                        Logger?.Invoke($"[Server]: Error - {ex.Message}.");
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                ServerError?.Invoke(ex);
            }
            finally
            {
                try { await StopAsync();}
                catch { }
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
                session.CloseSession();

            _sessions.Clear();

            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;

            Logger?.Invoke("[Server]: Stopped.");
        }

        private void StopListening()
        {
            try
            {
                _listener?.Stop();
            }
            catch (Exception ex)
            {
                Logger?.Invoke($"[Server]: Error while stopping the listener: {ex.Message}");
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

                session = _sessionFactory!(topClient, Context, Logger);

                if (session == null)
                    return;

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
                Logger?.Invoke($"[Server]: Error handling client [{topClient!.RemoteEndPoint}] - {ex.Message}");
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
