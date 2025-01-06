
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using WPFTasks.Core.Models.Currency;
using WPFTasks.ViewModels;

namespace WPFTasks.Core.ViewModels
{
    public class ChatMessage
    {
        public string Sender { get; set; }
        public string Content { get; set; }
    }

    public class CurrencyClientViewModel : INotifyPropertyChanged
    {
        private readonly CurrencyClient _currencyClient;

        // Поля для подключения
        private string _ipAddress = "127.0.0.1";
        private string _port = "18080";
        private string _login;
        private string _password;
        private string _newMessage;

        // Состояние
        private bool _areHintsVisible;
        private ObservableCollection<string> _hints = [];

        // Чат и сообщения
        public ObservableCollection<ChatMessage> Messages { get; } = [];

        // Свойства
        public string IpAddress
        {
            get => _ipAddress;
            set { _ipAddress = value; OnPropertyChanged(nameof(IpAddress)); }
        }

        public string Port
        {
            get => _port;
            set { _port = value; OnPropertyChanged(nameof(Port)); }
        }

        public string Login
        {
            get => _login;
            set { _login = value; OnPropertyChanged(nameof(Login)); }
        }

        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(nameof(Password)); }
        }

        public string NewMessage
        {
            get => _newMessage;
            set
            {
                _newMessage = value;
                UpdateHints();
                OnPropertyChanged(nameof(NewMessage));
            }
        }

        public bool AreHintsVisible
        {
            get => _areHintsVisible;
            set { _areHintsVisible = value; OnPropertyChanged(nameof(AreHintsVisible)); }
        }

        public ObservableCollection<string> Hints
        {
            get => _hints;
            set { _hints = value; OnPropertyChanged(nameof(Hints)); }
        }

        // Команды
        public ICommand ConnectCommand { get; }
        public ICommand SendMessageCommand { get; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnDsa()
        {
            ShowMessageBox("Соединение потеряно." + CanConnect());

            bool sad = !_currencyClient.IsConnected;
        }
        public CurrencyClientViewModel()
        {
            _currencyClient = new CurrencyClient();

            // Подписываемся на события
            _currencyClient.OnEndSession += () 
                => ShowMessageBox("Сессия завершена.");
            _currencyClient.OnConnectionLost += OnDsa;
            _currencyClient.OnAuthenticationResponse += response =>
            {
                var message = response.IsAuthenticated ? "Аутентификация успешна." : "Ошибка аутентификации.";
                ShowMessageBox(message);
            };
            _currencyClient.OnCurrencyResponse += response 
                => ShowMessageBox($"Курс валют: {response.FromCurrency} → {response.ToCurrency}: {response.Rate}");
            _currencyClient.OnErroreOnClient += error 
                => ShowMessageBox($"Ошибка клиента: {error}");
            _currencyClient.OnErroreFromServer += error 
                => ShowMessageBox($"Ошибка сервера: [{error.Payload}].");
            _currencyClient.OnServerOverloaded += () 
                => ShowMessageBox("Сервер перегружен.\nНевозможно подключиться, попробуйте позже...");

            // Инициализация команд
            ConnectCommand = new RelayCommand(async _ => await ConnectAsync(), _ => CanConnect());
            SendMessageCommand = new RelayCommand(async _ => await SendMessageAsync(), _ => CanSendMessage());
        }

        private void ShowMessageBox(string message)
        {
            Application.Current.Dispatcher.Invoke(() => MessageBox.Show(message, "Информация", MessageBoxButton.OK, MessageBoxImage.Information));
        }

        private bool CanConnect() => !string.IsNullOrWhiteSpace(IpAddress) && int.TryParse(Port, out _) && !_currencyClient.IsConnected;

        private async Task ConnectAsync()
        {
            try
            {
                _currencyClient.Connect(IpAddress, int.Parse(Port));
                ShowMessageBox("Подключение успешно.");
            }
            catch (Exception ex)
            {
                ShowMessageBox($"Ошибка подключения: {ex.Message}");
            }
            finally {

            }
        }

        private bool CanSendMessage() => !string.IsNullOrWhiteSpace(NewMessage) && _currencyClient.IsConnected;

        private async Task SendMessageAsync()
        {
            string input = NewMessage.TrimEnd();
            Messages.Add(new ChatMessage { Sender = "Вы", Content = NewMessage });
            NewMessage = string.Empty;

            try
            {
                if (input.StartsWith("/Rate", StringComparison.CurrentCultureIgnoreCase))
                {
                    var parts = input.Split(' ');
                    if (parts.Length == 3)
                    {
                        await _currencyClient.SendCurrencyRequest(parts[1], parts[2]);
                    }
                    else
                    {
                        ShowMessageBox("Команда /Rate должна быть в формате: {/Rate <FromCurrency> <ToCurrency>}");
                    }
                }
                if (input.StartsWith("/Auth", StringComparison.CurrentCultureIgnoreCase))
                {
                    var parts = input.Split(" ");
                    if (parts.Length == 3)
                    {
                        await _currencyClient.SendAuthRequest(parts[1], parts[2]);
                    }
                    else
                    {
                        ShowMessageBox("Команда /auth должна быть в формате: {/auth <Login> <Password>}");
                    }
                }
                if (input.StartsWith("/Out", StringComparison.CurrentCultureIgnoreCase))
                {
                    await _currencyClient.SendCloseSessionRequest();
                }
                if (input.StartsWith("/Break", StringComparison.CurrentCultureIgnoreCase))
                {
                    _currencyClient.Disconnect();
                }
            }
            catch (Exception ex)
            {
                ShowMessageBox(ex.Message);
            }

        }

        private void UpdateHints()
        {
            Hints.Clear();

            if (!string.IsNullOrWhiteSpace(NewMessage) && NewMessage.StartsWith('/'))
            {
                var allHints = new Dictionary<string, string>
                {
                    { "/rate", "/Rate <FromCurrency> <ToCurrency>" },
                    { "/auth", "/Auth <Login> <Password>" },
                    { "/out", "/Out" },
                    { "/break", "/Break" },
                };
                string input = NewMessage.ToLower();

                foreach (var hint in allHints)
                {
                    int minLenght = Math.Min(input.Length, hint.Key.Length);
                    for (int i = 0; i < minLenght; i++)
                    {
                        if (input[i].Equals(hint.Key[i]))
                        {
                            if (i == minLenght - 1)
                                Hints.Add(hint.Value);

                            continue;
                        }
                        break;
                    }
                }
            }

            AreHintsVisible = Hints.Any();
        }


        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
