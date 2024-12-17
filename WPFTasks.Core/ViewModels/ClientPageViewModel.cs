
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
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
        private static CurrencyClient _currencyClient = null!;
        private readonly Dispatcher _dt;

        public ClientPageViewModel(Dispatcher dt)
        {
            CurrencyClient.CurrentDispatcher = dt;
            _dt = dt;

            _currencyClient ??= new CurrencyClient();

            _currencyClient.OnAuthenticated += OnAuthenticated;
            _currencyClient.OnGetErrorMsg += OnError;
            _currencyClient.OnGetCurrencyRateMsg += OnGetCurrencyRateMsg;

            ConnectCommand = new RelayCommand(async _ => await ConnectAsync(), (_) => !IsConnected);
            SendMessageCommand = new RelayCommand(SendMessage, _ => IsConnected && !String.IsNullOrWhiteSpace(NewMessage));

            _currencyClient.OnGetEndSessionNotification += () => { 
                MessageBox.Show("Время сессии кончилось.\nВойдите заного.", "Уведомление", MessageBoxButton.OK, MessageBoxImage.Information);
                IsConnected = false;
                _currencyClient!.Close();

                _dt.Invoke(() => {
                    ((RelayCommand)SendMessageCommand).RaiseCanExecuteChanged();
                    ((RelayCommand)ConnectCommand).RaiseCanExecuteChanged();
                });
            };

            Messages = [];
        }

        private string _ipAddress = "127.0.0.1";
        public string IpAddress
        {
            get => _ipAddress;
            set => SetProperty(ref _ipAddress, value);
        }

        private string _port = "8280";
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
            set { SetProperty(ref _newMessage, value); ((RelayCommand)SendMessageCommand).RaiseCanExecuteChanged(); }
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
                if(_currencyClient!.)
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
            try
            {
                string text = NewMessage.Trim();

                if (text.ToLower() == "cls")
                {
                    _dt.Invoke(() => { Messages.Clear(); });
                    return;
                }
                string[] words = text.Split(' ');
                if (words.Length == 2)
                {
                    _ = _currencyClient.RequestCurrencyRate(words[0].ToUpper(), words[1].ToUpper());
                    AddMessage(Login, NewMessage);
                }
                else
                {
                    MessageBox.Show("Нужно вести 2 слова. Пример: USD EUR");
                }
            }
            catch(Exception ex)
            {
                AddMessage("System", ex.Message);
            }
            finally
            {
                NewMessage = string.Empty;
            }
        }

        private void AddMessage(string sender, string content)
        {
            _dt.Invoke(() =>
            {
                Messages.Add(new MessageViewModel { Sender = sender, Content = content });
            });
        }

        private void OnAuthenticated(bool isAuthenticated, string payload)
        {
            _dt.Invoke(() => {
                IsConnected = isAuthenticated;

                ((RelayCommand)SendMessageCommand).RaiseCanExecuteChanged();
                ((RelayCommand)ConnectCommand).RaiseCanExecuteChanged();

                AddMessage("System", payload);
            });
        }

        private void OnError(string error)
        {
            AddMessage("System", error);
        }

        private void OnGetCurrencyRateMsg(string from, string to, double rate)
        {
            _dt.Invoke(() =>
                AddMessage(
                    "Currency Rate", 
                    $"[{from}] -> [{to}] = {rate}"
                )
            );
        }
    }
}
