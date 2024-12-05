using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
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
        private string _login = "User";

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

        public string Login
        {
            get => _login;
            set => SetProperty(ref _login, value);
        }

        public ObservableCollection<ChatMessage> Messages { get; set; } = new ObservableCollection<ChatMessage>();
        public string NewMessage
        {
            get => _newMessage;
            set => SetProperty(ref _newMessage, value);
        }

        public bool IsConnected
        {
            get => _isConnected;
            set => SetProperty(ref _isConnected, value);
        }

        public ICommand ConnectCommand { get; }
        public ICommand SendMessageCommand { get; }

        public ChatViewModel()
        {
            _serverModel = new ChatServerModel();
            _serverModel.MessageReceived += OnMessageReceived;

            ConnectCommand = new RelayCommand(ConnectToServer, (object a) => !IsConnected);
            SendMessageCommand = new RelayCommand(async () => await SendMessage(), (object a) => IsConnected && !string.IsNullOrWhiteSpace(NewMessage));
        }

        private async void ConnectToServer()
        {
            try
            {
                await _serverModel.ConnectAsync(IpAddress, Port, Login);
                IsConnected = true;
                Messages.Add(new ChatMessage { Sender = "System", Content = "Connected to server." });
            }
            catch (Exception ex)
            {
                Messages.Add(new ChatMessage { Sender = "System", Content = $"Connection failed: {ex.Message}" });
            }
        }

        private async Task SendMessage()
        {
            try
            {
                await _serverModel.SendMessageAsync(NewMessage);
                Messages.Add(new ChatMessage { Sender = Login, Content = NewMessage });
                NewMessage = string.Empty;
            }
            catch (Exception ex)
            {
                Messages.Add(new ChatMessage { Sender = "System", Content = $"Send failed: {ex.Message}" });
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
