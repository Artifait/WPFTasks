
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using WPFTasks.Core.Models.Currency;
using WPFTasks.ViewModels;

namespace WPFTasks.Core.ViewModels
{
    public class MessageViewModel
    {
        public string Sender { get; set; }
        public string Content { get; set; }
    }

    public class ClientPageViewModel : BaseViewModel
    {
        private readonly CurrencyClient _currencyClient;

        public ClientPageViewModel()
        {
            _currencyClient = new CurrencyClient();
            _currencyClient.OnAuthenticated += OnAuthenticated;
            _currencyClient.OnGetErrorMsg += OnError;
            _currencyClient.OnGetCurrencyRateMsg += OnGetCurrencyRateMsg;

            ConnectCommand = new RelayCommand(async _ => await ConnectAsync(), _ => !IsConnected);
            SendMessageCommand = new RelayCommand(SendMessage, _ => !string.IsNullOrWhiteSpace(NewMessage) && IsConnected);

            Messages = [];
        }

        private string _ipAddress = "127.0.0.1";
        public string IpAddress
        {
            get => _ipAddress;
            set => SetProperty(ref _ipAddress, value);
        }

        private string _port = "8080";
        public string Port
        {
            get => _port;
            set => SetProperty(ref _port, value);
        }

        private string _login = "user1";
        public string Login
        {
            get => _login;
            set => SetProperty(ref _login, value);
        }

        private string _password = "a";
        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        private string _newMessage;
        public string NewMessage
        {
            get => _newMessage;
            set => SetProperty(ref _newMessage, value);
        }

        public ObservableCollection<MessageViewModel> Messages { get; }

        public ICommand ConnectCommand { get; }
        public ICommand SendMessageCommand { get; }

        private bool _isConnected;
        public bool IsConnected
        {
            get => _isConnected;
            private set => SetProperty(ref _isConnected, value);
        }

        private async Task ConnectAsync()
        {
            try
            {
                await _currencyClient.Init(IpAddress, int.Parse(Port), Login, Password);
            }
            catch (Exception ex)
            {
                AddMessage("System", ex.Message);
            }
            finally
            {
            }
        }

        private void SendMessage(object _)
        {
            AddMessage("You", NewMessage);
            NewMessage = string.Empty;
        }

        private void AddMessage(string sender, string content)
        {
            Messages.Add(new MessageViewModel { Sender = sender, Content = content });
        }

        private void OnAuthenticated(bool isAuthenticated)
        {
            IsConnected = isAuthenticated;

            if (isAuthenticated)
            {
                AddMessage("System", "Successfully authenticated.");
            }
            else
            {
                AddMessage("System", "Authentication failed.");
            }
        }

        private void OnError(string error)
        {
            AddMessage("System", error);
        }

        private void OnGetCurrencyRateMsg(string from, string to, double rate)
        {
            AddMessage(
                "Currency Rate", 
                $"[{from}] -> [{to}] = {rate}"
            );
        }
    }
}
