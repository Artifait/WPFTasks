
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
        private Func<TopClient, ServiceRegistry, LogString?, Task<ClientSession?>>? _sessionFactory;
        private CancellationTokenSource? _cancellationTokenSource;
        private IPEndPoint? _currentEndPoint;
        private TcpListener? _listener;

        // События
        public event Action<TopClient>? ClientConnected;
        public event Action<TopClient>? ClientDisconnected;
        public event Action<Exception>? ServerError;
        public event Action<ClientSession, Message>? OnMessageProcessed;
        public LogString? Logger { get; set; }
        public ServiceRegistry Context { get; private set; } = new();
        public EndPoint? CurrentEndPoint => _currentEndPoint;
        public int CountOpenSessions => _sessions.Count;
        public bool IsRunning => _cancellationTokenSource != null;

        // Сеттеры для зависимостей
        public RrServer SetEndPoint(IPEndPoint endPoint)
        {
            _currentEndPoint = endPoint ?? throw new ArgumentNullException(nameof(endPoint));
            _listener = new TcpListener(_currentEndPoint);
            return this;
        }

        public RrServer SetSessionFactory(Func<TopClient, ServiceRegistry, LogString?, Task<ClientSession?>> sessionFactory)
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
            if (IsRunning)
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
                        var tcpClient = await _listener.AcceptTcpClientAsync(_cancellationTokenSource.Token);
                        _ = Task.Run(() => HandleNewClientAsync(tcpClient), _cancellationTokenSource.Token);
                    }
                    catch(OperationCanceledException ex) {
                        Logger?.Invoke($"[Server]: Остановка прослушки.");
                    }
                    catch (Exception ex) {
                        Logger?.Invoke($"[Server]: Ошибка - {ex.Message}.");
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

                session = await _sessionFactory!(topClient, Context, Logger);

                if (session == null)
                {
                    topClient.Disconnect();
                    return;
                }

                if (!_sessions.TryAdd(clientGuid, session))
                {
                    throw new InvalidOperationException("Failed to add client session.");
                }

                ClientConnected?.Invoke(topClient);
                Logger?.Invoke($"[{session.RemoteEndPoint}]: Клиент подключился...");
                session.logger = Logger;
                session.OnMessageProcessed += Session_OnMessageProcessed;
                await session.StartAsync();
                _sessions.TryRemove(clientGuid, out _);
            }
            catch (Exception ex)
            {
                ServerError?.Invoke(ex);
                Logger?.Invoke($"[Server]: Ошибка обработки клиента [{session?.RemoteEndPoint}] - {ex.Message}");
            }
            finally
            {
                if (session != null)
                {
                    session.CloseSession();
                    session.OnMessageProcessed -= Session_OnMessageProcessed;
                    Logger?.Invoke($"[{session.RemoteEndPoint}]: Клиент отключился...");
                    ClientDisconnected?.Invoke(topClient!);
                }
            }
        }

        private void Session_OnMessageProcessed(ClientSession arg1, Message arg2)
        {
            Logger?.Invoke($"[Server]: Отправка ответа клиенту [{arg1.RemoteEndPoint}] с типом сообщения: {arg2.MessageType}...");
            OnMessageProcessed?.Invoke(arg1, arg2);
        }
    }
}
