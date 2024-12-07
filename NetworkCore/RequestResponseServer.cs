
using System.Net;
using TopNetwork.Core.Defaults;

namespace TopNetwork.Core
{
    public class RequestResponseServer : DefaultServer
    {
        // События
        public event Action<TopClient>? ClientConnected;
        public event Action<TopClient>? ClientDisconnected;
        public event Func<TopClient, Task>? ClientRejected;

        // Обработчики
        public Func<TopClient?, Task<bool>>? ShouldAcceptClient;
        public Func<TopClient, Task<Message?>>? BuilderDisconnectMessage;

        protected override async Task OnStartAsync(CancellationToken cancellationToken)
        {
            if (ServerHandlers == null)
                throw new NullReferenceException($"Plz init server");

            Console.WriteLine("Server started.");
            _ = AcceptClientsAsync(cancellationToken); // Запуск цикла приёма клиентов
        }

        protected override Task OnStopAsync()
        {
            Console.WriteLine("Server stopped.");
            return Task.CompletedTask;
        }

        private async Task AcceptClientsAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    TopClient client = new(await Listener.AcceptTcpClientAsync(cancellationToken));
                    if (await ShouldAccept(client))
                    {
                        ClientConnected?.Invoke(client);
                        _ = HandleClientAsync(client, cancellationToken); // Обработка клиента в отдельной задаче
                    }
                    else
                    {
                        await RejectClient(client); // Отклонение клиента
                    }
                }
                catch (Exception ex) when (ex is ObjectDisposedException or OperationCanceledException)
                {
                    Console.WriteLine("Stopped accepting clients.");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error accepting client: {ex.Message}");
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
                    Console.WriteLine("Client disconnected by event.");
                }
                else if (cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine("Client disconnected by cancellation.");
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
                Console.WriteLine("Client handling stopped.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error handling client: {ex.Message}");
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

            return client != null && client.RemoteEndPoint is IPEndPoint;
        }

        private async Task RejectClient(TopClient client)
        {
            if (ClientRejected != null)
                await ClientRejected.Invoke(client);

            Console.WriteLine("Client rejected.");
            client.Close();
        }
    }
}
