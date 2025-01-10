
using TopNetwork.RequestResponse;
using TopNetwork.Services.MessageBuilder;

namespace WPFTasks.Core.Models.Core
{
    public abstract class BaseClient
    {
        protected readonly RrClientHandlerBase Handlers;
        protected readonly RrClient Client;
        protected readonly MessageBuilderService MessageBuilderService;

        public event Action<ErroreData>? OnErroreFromServer;
        public event Action<string>? OnErroreOnClient;
        public event Action? OnServerOverloaded;
        public event Action? OnConnectionLost;
        public bool IsInitialized => Client?.IsInitialized ?? false;
        public bool IsConnected => Client?.IsConnected ?? false;

        protected BaseClient()
        {
            Handlers = new RrClientHandlerBase();
            Client = new RrClient(Handlers);
            MessageBuilderService = new MessageBuilderService();
            Client.ServiceRegistry.Register(MessageBuilderService); 
            Client.OnConnectionLost += InvokeOnConnectionLost;

            RegisterMessageBuilders();
            RegisterMessageHandlers();
        }

        protected abstract void RegisterMessageBuilders();

        protected abstract void RegisterMessageHandlers();

        public void Connect(string ip, int port) => Client.Connect(ip, port);

        public async Task ConnectAsync(string ip, int port) => await Client.ConnectAsync(ip, port);

        public void Disconnect() => Client.Disconnect();

        public async Task SendMessageAsync<TBuilder, TData>(Action<TBuilder> setupAction = null)
            where TBuilder : IMessageBuilder<TData>, new()
            where TData : IMsgSourceData
        {
            try
            {
                var message = MessageBuilderService.BuildMessage<TBuilder, TData>(setupAction);
                await Client.SendMessageWithoutResponseAsync(message);
            }
            catch (Exception ex)
            {
                OnErroreOnClient?.Invoke($"Error sending message: {ex.Message}");
            }
        }

        protected void InvokeOnErroreOnClient(string message)
            => OnErroreOnClient?.Invoke(message);

        protected void InvokeOnErroreFromServer(ErroreData erroreData)
            => OnErroreFromServer?.Invoke(erroreData);

        protected void InvokeOnServerOverloaded()
            => OnServerOverloaded?.Invoke();

        protected void InvokeOnConnectionLost()
            => OnConnectionLost?.Invoke();
    }
}
