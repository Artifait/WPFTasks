
using System.Collections.Concurrent;
using TopNetwork.Core;

namespace TopNetwork.RequestResponse
{
    /// <summary>
    /// Rr -> Request Response Client
    /// </summary>
    public class RrClient
    {
        private readonly TopClient _topClient;
        private readonly ConcurrentDictionary<string, TaskCompletionSource<Message>> _pendingResponses = new();

        public RrClientHandlerBase? Handler { get; private set; } = null;
        public ServiceRegistry ServiceRegistry { get; private set; }
        public bool IsActive => _topClient?.IsConnected ?? false;

        public RrClient(TopClient topClient, RrClientHandlerBase? handler = null)
        {
            _topClient = topClient ?? throw new ArgumentNullException(nameof(topClient));
            Handler = handler;

            _topClient.OnMessageReceived += HandleIncomingMessageAsync;
        }

        /// <param name="message"> Сообщение для отправки </param>
        /// <param name="cancellationToken"> токен отмены </param>
        /// <returns> Ответ от сервера => не желательно использовать синхроно </returns>
        /// <exception cref="ArgumentNullException"> если message == null</exception>
        public async Task<Message?> SendMessageWithResponseAsync(Message message, CancellationToken cancellationToken = default)
        {
            if(!IsActive) 
                throw new InvalidOperationException("Невозможно отправить сообщение из не активного состояния...");

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
            if (!IsActive)
                throw new InvalidOperationException("Невозможно отправить сообщение из не активного состояния...");

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
    }
}