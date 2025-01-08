
using System.Collections.Concurrent;
using TopNetwork.Core;

namespace TopNetwork.RequestResponse
{
    /// <summary>
    /// Rr -> Request Response Client
    /// </summary>
    public class RrClient
    {
        private TopClient _topClient;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<Message>> _pendingResponses = new();

        public RrClientHandlerBase? Handler { get; private set; } = null;
        public ServiceRegistry ServiceRegistry { get; set; } = new();
        public bool IsConnected => _topClient?.IsConnected ?? false;
        public bool IsInitialized => _topClient?.IsInitialized ?? false;

        public event Action? OnConnectionLost;

        public RrClient(RrClientHandlerBase? handler = null)
        {
            Handler = handler;
        }

        public async Task<RrClient> ConnectAsync(string ip, int port)
        {
            if(_topClient != null)
            {
                _topClient.OnMessageReceived -= HandleIncomingMessageAsync;
                _topClient.OnConnectionLost -= OnConnectionLostEvent;
                _topClient.Disconnect();
            }

            _topClient = await new TopClient().ConnectAsync(ip, port);
            _topClient.OnConnectionLost += OnConnectionLostEvent;
            _topClient.OnMessageReceived += HandleIncomingMessageAsync;
            _ = StartListening();
            return this;
        }

        public RrClient Connect(string ip, int port)
        {
            if (_topClient != null)
            {
                _topClient.OnMessageReceived -= HandleIncomingMessageAsync;
                _topClient.OnConnectionLost -= OnConnectionLostEvent;
                _topClient.Disconnect();
            }

            _topClient = new TopClient().Connect(ip, port);
            _topClient.OnConnectionLost += OnConnectionLostEvent;
            _topClient.OnMessageReceived += HandleIncomingMessageAsync;
            _ = StartListening();
            return this;
        }

        public void Disconnect()
        {
            _topClient?.Disconnect();
        }

        public async Task StartListening()
        {
            if (!IsConnected)
                throw new InvalidOperationException("Сначало нужно подключиться к серверу...");

            await _topClient.StartListeningAsync();
        }

        public void StopListening() => _topClient.StopListening();

        /// <param name="message"> Сообщение для отправки </param>
        /// <param name="cancellationToken"> токен отмены </param>
        /// <returns> Ответ от сервера => не желательно использовать синхроно </returns>
        /// <exception cref="ArgumentNullException"> если message == null</exception>
        public async Task<Message?> SendMessageWithResponseAsync(Message message, CancellationToken cancellationToken = default)
        {
            if(!IsConnected) 
                throw new InvalidOperationException("Невозможно отправить сообщение из не подключеного состояния...");

            ArgumentNullException.ThrowIfNull(message);

            string messageId = Guid.NewGuid().ToString();
            message.Headers["MessageId"] = messageId;

            var tcs = new TaskCompletionSource<Message>();
            _pendingResponses[messageId] = tcs;

            try
            {
                await _topClient.SendMessageAsync(message, cancellationToken);

                // Ожидание ответа или отмены
                using (cancellationToken.Register(() => tcs.TrySetCanceled()))
                {
                    return await tcs.Task;
                }
            }
            finally
            {
                _pendingResponses.TryRemove(messageId, out _);
            }
        }
        public async Task SendMessageWithoutResponseAsync(Message message, CancellationToken cancellationToken = default)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Невозможно отправить сообщение из не подключеного состояния...");

            ArgumentNullException.ThrowIfNull(message);
            await _topClient.SendMessageAsync(message, cancellationToken);
        }

        private async Task HandleIncomingMessageAsync(Message message)
        {
            if (message == null)
                return;

            try
            {
                // Проверьте, является ли сообщение ответом
                if (message.Headers.TryGetValue("ResponseTo", out var messageId) && _pendingResponses.TryRemove(messageId, out var tcs))
                {
                    tcs.TrySetResult(message);
                }
                else
                {
                    if (Handler == null)
                        return;

                    // Если нет подписчика или заголовка "ResponseTo", обработать его с помощью обработчика
                    var response = await Handler.HandleMessage(message);
                    if (response != null)
                    {
                        await _topClient.SendMessageAsync(response);
                    }
                }
            }
            catch { }
        }

        private void OnConnectionLostEvent()
        {
            OnConnectionLost?.Invoke();
        }
    }
}