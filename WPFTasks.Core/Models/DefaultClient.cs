
using System.Windows.Threading;
using TopNetwork.Core;

namespace WPFTasks.Core.Models
{
    public abstract class DefaultClient
    {
        protected readonly SemaphoreSlim InitSemaphore = new(1, 1);
        protected TopClient? _client;
        protected CancellationTokenSource? _cts;
        protected ClientHandlerBase Handlers;


        private bool _authenticated = false;
        public bool Authenticated
        {
            get => _authenticated;
            protected set => _authenticated = value;
        }

        private bool _isInitialized = false;
        public bool IsInitialized
        {
            get => _isInitialized;
            protected set => _isInitialized = value;
        }

        public Action<string>? OnErrorOccurred;
        public Action OnDisconnected;

        protected DefaultClient()
        {
            Handlers = new ClientHandlerBase();
            InitializeHandlers();
        }

        /// <summary>
        /// Абстрактный метод для настройки обработчиков сообщений, специфичных для сервера.
        /// </summary>
        protected abstract void InitializeHandlers();

        /// <summary>
        /// Инициализирует клиента и подключается к серверу.
        /// </summary>
        public async Task InitAsync(string serverIp, int port)
        {
            await InitSemaphore.WaitAsync();
            try
            {
                if (IsInitialized)
                    await TryDisconnectAsync();

                _client = new TopClient(serverIp, port);
                _cts = new CancellationTokenSource();

                _client.OnAcceptedMessage += Handlers.HandleMessage;
                _client.OnDisconnected += HandleDisconnection;

                IsInitialized = true;

                _ = _client.StartListen(_cts.Token);
            }
            catch (Exception ex)
            {
                OnErrorOccurred?.Invoke($"Ошибка инициализации клиента: {ex.Message}");
            }
            finally
            {
                InitSemaphore.Release();
            }
        }

        /// <summary>
        /// Отправляет сообщение на сервер.
        /// </summary>
        protected async Task SendMessageAsync(Message message)
        {
            if (_client == null || !_client.IsConnected)
                throw new InvalidOperationException("Клиент не подключен.");

            await _client.SendMessageAsync(message);
        }

        /// <summary>
        /// Попытка отключиться от сервера.
        /// </summary>
        public async Task TryDisconnectAsync()
        {
            try
            {
                if (_client?.IsConnected ?? false)
                {
                    Message? msg = CreateCloseSessionMessage();
                    if (msg != null)
                        await SendMessageAsync(msg);

                    _cts?.Cancel();
                    _cts?.Dispose();
                    _client?.Disconnect();
                }
            }
            catch (Exception ex)
            {
                OnErrorOccurred?.Invoke($"Ошибка при отключении: {ex.Message}");
            }
            finally
            {
                IsInitialized = false;
                Authenticated = false;
            }
        }

        /// <summary>
        /// Обработчик отключения клиента.
        /// </summary>
        private void HandleDisconnection()
        {
            Authenticated = false;
            OnDisconnected?.Invoke();
        }

        /// <summary>
        /// Метод для создания сообщения о закрытии сессии. 
        /// Реализация должна быть определена в дочерних классах.
        /// </summary>
        protected abstract Message? CreateCloseSessionMessage();

        /// <summary>
        /// Закрывает соединение.
        /// </summary>
        public void Close()
        {
            _client?.Close();
        }
    }
}