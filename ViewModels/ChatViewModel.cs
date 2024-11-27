using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using WPFTasks.Models;

namespace WPFTasks.ViewModels
{
    public class ChatViewModel : BaseViewModel
    {
        private readonly ChatServerModel _serverModel;
        private string _newMessage;
        private bool _isConnected;

        private string _ipAddress = "127.0.0.1";
        private int _port = 58888;
        private string _username = "User";
        private string _password = "Password";

        public string IpAddress
        {
            get => _ipAddress;
            set => SetProperty(ref _ipAddress, value);
        }

        public int Port
        {
            get => _port;
            set => SetProperty(ref _port, value);
        }

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public ObservableCollection<ChatMessage> Messages { get; set; } = new ObservableCollection<ChatMessage>();

        public bool IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }

        public ICommand ConnectCommand { get; }
        public ICommand RequestQuoteCommand { get; }

        public ChatViewModel()
        {
            _serverModel = new ChatServerModel();
            _serverModel.MessageReceived += OnMessageReceived;

            ConnectCommand = new RelayCommand(ConnectToServer, () => !IsConnected);
            RequestQuoteCommand = new RelayCommand(async () => await RequestQuote(), () => IsConnected);
        }

        private async void ConnectToServer()
        {
            try
            {
                await _serverModel.ConnectAsync(IpAddress, Port, Username, Password);
                IsConnected = true;
                Messages.Add(new ChatMessage { Sender = "System", Content = "Connected to server." });
            }
            catch (Exception ex)
            {
                Messages.Add(new ChatMessage { Sender = "System", Content = $"Connection failed: {ex.Message}" });
            }
        }
        private async Task GetQuoteAsync()
        {
            try
            {
                if (_client?.Connected == true)
                {
                    await _writer.WriteLineAsync("next");
                    var quote = await _reader.ReadLineAsync();
                    LogMessages.Add($"Quote: {quote}");
                }
            }
            catch (Exception ex)
            {
                LogMessages.Add($"Error: {ex.Message}");
            }
        }
        private async Task RequestQuote()
        {
            try
            {
                await _serverModel.RequestQuoteAsync();
            }
            catch (Exception ex)
            {
                Messages.Add(new ChatMessage { Sender = "System", Content = $"Request failed: {ex.Message}" });
            }
        }

        private void OnMessageReceived(string message)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                Messages.Add(new ChatMessage { Sender = "Server", Content = message });
            });
        }
    }
}
