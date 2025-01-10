
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using WPFTasks.Core.Models.Core;

namespace WPFTasks.Core.ViewModels.Core
{
    public abstract class ClientViewModel : HintOnMessageInputBoxBaseVm
    {
        protected readonly BaseClient Client;

        // Поля для подключения
        private string _ipAddress = "127.0.0.1";
        private string _port = "18080";

        // Чат и сообщения
        public ObservableCollection<ChatMessage> Messages { get; } = [];

        // Свойства
        public string IpAddress
        {
            get => _ipAddress;
            set => SetProperty(ref _ipAddress, value);
        }

        public string Port
        {
            get => _port;
            set => SetProperty(ref _port, value);
        }

        public ICommand ConnectCommand { get; }
        public ICommand SendMessageCommand { get; }
        public event Action? OnUpdateMessages;

        protected ClientViewModel(BaseClient client)
        {
            Client = client;

            ConnectCommand = new RelayCommand(async _ => await ConnectAsync(), _ => CanConnect());
            SendMessageCommand = new RelayCommand(async _ => await SendMessageAsync(), _ => CanSendMessage());

            Client.OnConnectionLost += () => AddMessage("Client", "Соединение потеряно.");
            Client.OnErroreOnClient += error => AddMessage("Client", $"Ошибка: {error}");
            Client.OnErroreFromServer += error => AddMessage("Server", $"Ошибка: [{error.Payload}]");
            Client.OnServerOverloaded += () => AddMessage("ServerResponse", "Сервер перегружен.");
        }

        private async Task SendMessageAsync()
        {
            string input = NewMessage.TrimEnd();
            AddMessage("Вы", NewMessage);
            NewMessage = string.Empty;

            try
            {
                await ExecuteCommandAsync(input);
            }
            catch (Exception ex)
            {
                AddMessage("_commandProcessor", ex.Message);
            }
        }

        protected abstract Task ExecuteCommandAsync(string input);

        private async Task ConnectAsync()
        {
            try
            {
                await ConnectAsync(IpAddress, int.Parse(Port));
            }
            catch (Exception ex)
            {
                AddMessage("Server", $"Ошибка подключения: {ex.Message}");
            }
        }

        private async Task ConnectAsync(string ipAddress, int port)
        {
            try
            {
                await Client.ConnectAsync(ipAddress, port);

                if (Client.IsConnected)
                    AddMessage("Server", "Подключение успешно.");
            }
            catch (Exception ex)
            {
                AddMessage("Server", $"Ошибка подключения: {ex.Message}");
            }
        }

        protected void ShowMessageBox(string message)
            => Application.Current.Dispatcher.Invoke(() => MessageBox.Show(message, "Информация", MessageBoxButton.OK, MessageBoxImage.Information));

        protected void AddMessage(string sender, string message)
            => Application.Current.Dispatcher.Invoke(() =>
            {
                Messages.Add(new ChatMessage { Sender = sender, Content = message });
                OnUpdateMessages?.Invoke();
            });

        private bool CanConnect() => !string.IsNullOrWhiteSpace(IpAddress) && int.TryParse(Port, out _) && !Client.IsConnected;

        private bool CanSendMessage() => !string.IsNullOrWhiteSpace(NewMessage);
    }
}
