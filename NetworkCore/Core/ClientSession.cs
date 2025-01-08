
using System.Collections.Concurrent;
using System.Net;
using TopNetwork.Conditions;
using TopNetwork.RequestResponse;

namespace TopNetwork.Core
{
    public class ClientSession
    {
        private readonly TopClient _client;
        private readonly EndPoint _clientEndPoint;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly object _stateLock = new();
        private readonly object _conditionsLock = new();

        /// <summary> Обработка сообщения клиента -> Здесь ответ от сервера </summary>
        public event Action<ClientSession, Message>? OnMessageProcessed;
        public event Action<ClientSession>? OnSessionStarted;
        public event Action<ClientSession>? OnSessionClosed;

        public SessionCloseConditionEvaluator PreMsgCloseConditionEvaluator { get; set; } = new();
        public SessionCloseConditionEvaluator PostMsgCloseConditionEvaluator { get; set; } = new();
        public SessionOpenConditionEvaluator OpenConditionEvaluator { get; set; } = new();
        public RrServerHandlerBase MessageHandlers { get; set; }
        public TopClient Client => _client;

        public readonly ServiceRegistry ServerContext;
        public LogString? logger;
        public bool IsRunning { get; private set; }
        public bool IsClosed { get; private set; } = false;
        public EndPoint? RemoteEndPoint => _clientEndPoint;
        public DateTime StartTime { get; private set; }
        public int ProcessedMessagesCountAll => ProcessedMessagesCountOfType.Values.Sum();
        public ConcurrentDictionary<string, int> ProcessedMessagesCountOfType { get; private set; } = new();

        public ClientSession(TopClient client, RrServerHandlerBase messageHandler, ServiceRegistry context)
        {
            _client = client ?? throw new ArgumentNullException(nameof(client));
            _clientEndPoint = _client.RemoteEndPoint ?? throw new ArgumentNullException(nameof(_client.RemoteEndPoint));
            MessageHandlers = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));
            _client.OnMessageReceived += HandleMessageAsync;
            _client.OnDisconnected += HandleClientDisconnected;
            ServerContext = context;
        }

        public async Task SendMessage(Message msg)
        {
            await _client.SendMessageAsync(msg);
        }

        public virtual async Task StartAsync()
        {
            try {
                if (!OpenConditionEvaluator.ShouldOpen(this))
                    return;
            }
            catch(Exception ex) {
                logger?.Invoke($"[Server.ClientSession]: Errore on checking OpenConditions - {ex.Message}.");
                return;
            }


            if (IsRunning)
            {
                logger?.Invoke($"[Server.ClientSession]: Errore - Session is already running.");
                return;
            }

            IsRunning = true;
            StartTime = DateTime.UtcNow;
            OnSessionStarted?.Invoke(this);

            try
            {
                await _client.StartListeningAsync(_cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                // Нормальное завершение
            }
            finally
            {
                CloseSession();
            }
        }

        public virtual void CloseSession()
        {
            lock (_stateLock)
            {
                if (IsClosed) return;
                IsClosed = true;
            }

            try
            {
                _client.Disconnect();
            }
            catch (Exception ex)
            {
                OnError($"[{RemoteEndPoint}]: Error while closing session - {ex.Message}");
            }
            finally
            {
                IsRunning = false;
                _cancellationTokenSource.Cancel();
                OnSessionClosed?.Invoke(this);
            }
        }

        private async Task HandleMessageAsync(Message message)
        {
            await CheckPreMsgCloseConditions();

            if (message == null) return;

            try
            {
                if (ProcessedMessagesCountOfType.TryGetValue(message.MessageType, out int cnt))
                    cnt++;
                else
                    ProcessedMessagesCountOfType[message.MessageType] = 1;

                var response = await MessageHandlers.HandleMessage(_client, message, ServerContext);

                if (response != null)
                {
                    if (message.Headers.TryGetValue("MessageId", out var msgId))
                        response.Headers["ResponseTo"] = msgId;

                    await _client.SendMessageAsync(response);
                    OnMessageProcessed?.Invoke(this, response);
                }
                await CheckPostMsgCloseConditions();
            }
            catch (Exception ex)
            {
                OnError($"[{RemoteEndPoint}]: Error processing message - {ex.Message}");
            }
        }

        private void HandleClientDisconnected()
        {
            CloseSession();
        }

        protected async Task CheckPreMsgCloseConditions()
        {
            if (await PreMsgCloseConditionEvaluator.ShouldCloseAsync(this))
            {
                lock (_conditionsLock)
                    CloseSession();
            }
        }
        protected async Task CheckPostMsgCloseConditions()
        {
            if (await PostMsgCloseConditionEvaluator.ShouldCloseAsync(this))
            {
                lock (_conditionsLock)
                    CloseSession();
            }
        }
        protected virtual void OnError(string errorMessage)
        {
            logger?.Invoke(errorMessage);
        }
    }
}
