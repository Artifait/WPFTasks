
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
            _dt = dt;
            _currencyClient ??= new CurrencyClient();

            _currencyClient.OnAuthenticated += OnAuthenticated;
            _currencyClient.OnErrorOccurred += OnError;
            _currencyClient.OnGetCurrencyRateMsg += OnGetCurrencyRateMsg;

            ConnectCommand = new RelayCommand(async _ => await ConnectAsync(), (_) => !_currencyClient.IsInitialized);
            SendMessageCommand = new RelayCommand(async _ => await SendMessage(), (_) => !String.IsNullOrWhiteSpace(NewMessage));

            _currencyClient.OnGetEndSessionNotification += () => { 
                MessageBox.Show("Время сессии кончилось.\nВойдите заного.", "Уведомление", MessageBoxButton.OK, MessageBoxImage.Information);
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
                await _currencyClient.InitAsync(IpAddress, int.Parse(Port));
                AddHelpMessage();
            }
            catch (Exception ex)
            {
                AddMessage("System", ex.Message);
            }
            finally
            {
                ((RelayCommand)ConnectCommand).RaiseCanExecuteChanged();
            }
        }
        private async Task SendMessage()
        {
            AddMessage(Login, NewMessage);

            try
            {
                string text = NewMessage.Trim();

                if (text.Equals("/cls", StringComparison.CurrentCultureIgnoreCase))
                {
                    _dt.Invoke(() => { Messages.Clear(); });
                    return;
                }
                if(text.Equals("/out", StringComparison.CurrentCultureIgnoreCase))
                {
                    await _currencyClient.TryDisconnectAsync();
                    ((RelayCommand)ConnectCommand).RaiseCanExecuteChanged();
                    return;
                }
                if(text.Equals("/help", StringComparison.CurrentCultureIgnoreCase))
                {
                    AddHelpMessage();
                    return;
                }
                string[] words = text.Split(' ');
                if(words.Length == 3)
                {
                    if (words[0].Equals("/auth", StringComparison.CurrentCultureIgnoreCase))
                    {
                        if(!_currencyClient.IsInitialized)
                        {
                            await _currencyClient.InitAsync(IpAddress, int.Parse(Port));
                        }
                        _ = _currencyClient.AuthenticateAsync(words[1], words[2]);
                    }
                    if (words[0].Equals("/rate", StringComparison.CurrentCultureIgnoreCase))
                    {
                        if (_currencyClient.Authenticated)
                            await _currencyClient.RequestCurrencyRateAsync(words[1], words[2]);
                        else
                            MessageBox.Show("Пройдите аутентификацию...", "Подсказка", MessageBoxButton.OK, MessageBoxImage.Information);

                    }
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
        private void AddHelpMessage()
        {
            AddMessage("System",
                "Сводка:\n" +
                "1) '/cls' - очистить чат.\n" +
                "2) '/auth <Login> <Password>' - авторизоваться.\n" +
                "3) '/out' - завершить сессию.\n" +
                "4) '/rate <FromCurrency> <ToCurrency>' - получить курс валют.");
        }

        private void OnAuthenticated(bool isAuthenticated, string payload)
        {
            _dt.Invoke(() => {
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
