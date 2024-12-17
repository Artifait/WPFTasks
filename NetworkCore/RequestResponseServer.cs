
using TopNetwork.Core.Defaults;

namespace TopNetwork.Core
{
    public delegate void LogString(string str);
    public class RequestResponseServer : DefaultServer
    {
        // События
        public event Action<TopClient>? ClientConnected;
        public event Action<TopClient>? ClientDisconnected;
        public event Func<TopClient, Task>? ClientRejected;

        // Обработчики
        public Func<TopClient?, Task<bool>>? ShouldAcceptClient;
        public Func<TopClient, Task<Message?>>? BuilderDisconnectMessage;
        public LogString? Logger { get; set; }

        public RequestResponseServer() { }
        public void Start()
        {
            if (Status.IsRunning)
                throw new InvalidOperationException("Сервер уже запущен.");
            if (Listener == null)
                throw new NullReferenceException("Инициализируйте");

            _cancellationTokenSource = new CancellationTokenSource();

            Status.StartTime = DateTime.Now;
            Status.IsRunning = true;

            Listener.Start();
            _ = OnStartAsync(_cancellationTokenSource.Token);
        }
        public override async Task OnStartAsync(CancellationToken cancellationToken)
        {
            if (ServerHandlers == null)
                throw new NullReferenceException($"Plz init server");

            Logger?.Invoke("Server started.");
            _ = AcceptClientsAsync(cancellationToken); // Запуск цикла приёма клиентов
        }


        public override async Task OnStopAsync()
        {
            Logger?.Invoke("Server stopped.");
        }

        private async Task AcceptClientsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    TopClient client = new(await Listener.AcceptTcpClientAsync(cancellationToken));
                    bool should = await ShouldAccept(client);
                    if (should)
                    {
                        ClientConnected?.Invoke(client);
                        Logger?.Invoke($"Client connected: {client.RemoteEndPoint}");

                        _ = HandleClientAsync(client, cancellationToken); // Обработка клиента в отдельной задаче    
                    }
                    else
                    {
                        await RejectClient(client); // Отклонение клиента
                    }
                }
                catch (Exception ex) when (ex is ObjectDisposedException or OperationCanceledException)
                {
                    Logger?.Invoke("Stopped accepting clients.");
                    break;
                }
                catch (Exception ex)
                {
                    Logger?.Invoke($"Error accepting client: {ex.Message}");
                }
            }
        }

        private async Task HandleClientAsync(TopClient client, CancellationToken cancellationToken)
        {
            try
            {
                // Подписка на получение сообщений
                client.OnAcceptedMessage += async message =>
                {
                    var response = await ServerHandlers.HandleMessage(client, message);
                    if (response != null)
                        await client.SendMessageAsync(response);
                };

                // Используем TaskCompletionSource для завершения работы клиента
                var disconnectCompletionSource = new TaskCompletionSource();

                // Подписываемся на событие отключения клиента
                client.OnDisconnected += () =>
                {
                    disconnectCompletionSource.TrySetResult(); // Сигнал завершения
                };

                // Запуск прослушивания сообщений
                _ = client.StartListen(cancellationToken);

                // Ждём либо отмену задачи, либо сигнал отключения
                var completedTask = await Task.WhenAny(
                    disconnectCompletionSource.Task,
                    Task.Delay(Timeout.Infinite, cancellationToken) // Бесконечное ожидание с отменой
                );

                if (completedTask == disconnectCompletionSource.Task)
                {
                    Logger?.Invoke("Client disconnected by event.");
                }
                else if (cancellationToken.IsCancellationRequested)
                {
                    Logger?.Invoke("Client disconnected by cancellation.");
                }

                // Отправка сообщения об отключении
                if (BuilderDisconnectMessage != null)
                {
                    var disconnectMessage = await BuilderDisconnectMessage(client);
                    if (disconnectMessage != null)
                        await client.SendMessageAsync(disconnectMessage);
                }
            }
            catch (Exception ex) when (ex is ObjectDisposedException or OperationCanceledException)
            {
                Logger?.Invoke("Client handling stopped.");
            }
            catch (Exception ex)
            {
                Logger?.Invoke($"Error handling client: {ex.Message}");
            }
            finally
            {
                client.Close();
                ClientDisconnected?.Invoke(client); // Вызов события отключения клиента
            }
        }

        private async Task<bool> ShouldAccept(TopClient? client)
        {
            if (ShouldAcceptClient != null)
                return await ShouldAcceptClient(client);

            return client != null;
        }

        private async Task RejectClient(TopClient client)
        {
            if (ClientRejected != null)
                await ClientRejected.Invoke(client);

            Logger?.Invoke($"Client rejected: {client.RemoteEndPoint}");
            client.Close();
        }
    }
}
