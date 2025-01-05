using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        private string _ipAddress;
        private string _port;
        private string _login;
        private string _password;
        private string _newMessage;

        // Состояние
        private bool _areHintsVisible;
        private ObservableCollection<string> _hints;

        // Чат и сообщения
        public ObservableCollection<ChatMessage> Messages { get; } = new ObservableCollection<ChatMessage>();

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
                OnPropertyChanged(nameof(NewMessage));
                UpdateHints();
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

        public CurrencyClientViewModel()
        {
            _currencyClient = new CurrencyClient();

            // Подписываемся на события
            _currencyClient.OnEndSession += () => ShowMessageBox("Сессия завершена.");
            _currencyClient.OnConnectionLost += () => ShowMessageBox("Соединение потеряно.");
            _currencyClient.OnAuthenticationResponse += response =>
            {
                var message = response.IsAuthenticated ? "Аутентификация успешна." : "Ошибка аутентификации.";
                ShowMessageBox(message);
            };
            _currencyClient.OnCurrencyResponse += response =>
                ShowMessageBox($"Курс валют: {response.FromCurrency} → {response.ToCurrency}: {response.Rate}");
            _currencyClient.OnErroreOnClient += error => ShowMessageBox($"Ошибка клиента: {error}");
            _currencyClient.OnErroreFromServer += error =>
                ShowMessageBox($"Ошибка сервера: [{error.Payload}].");
            _currencyClient.OnServerOverloaded += () => ShowMessageBox("Сервер перегружен.");

            // Инициализация команд
            ConnectCommand = new RelayCommand(async _ => await ConnectAsync(), _ => CanConnect());
            SendMessageCommand = new RelayCommand(async _ => await SendMessageAsync(), _ => CanSendMessage());
        }

        private void ShowMessageBox(string message)
        {
            Application.Current.Dispatcher.Invoke(() => MessageBox.Show(message, "Информация", MessageBoxButton.OK, MessageBoxImage.Information));
        }

        private bool CanConnect() => !string.IsNullOrWhiteSpace(IpAddress) && int.TryParse(Port, out _);

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
        }

        private bool CanSendMessage() => !string.IsNullOrWhiteSpace(NewMessage) && _currencyClient.IsConnected;

        private async Task SendMessageAsync()
        {
            if (NewMessage.StartsWith("/currency"))
            {
                var parts = NewMessage.Split(' ');
                if (parts.Length == 3)
                {
                    await _currencyClient.SendCurrencyRequest(parts[1], parts[2]);
                }
                else
                {
                    ShowMessageBox("Команда /currency должна быть в формате: /currency FROM TO.");
                }
            }
            else
            {
                Messages.Add(new ChatMessage { Sender = "Вы", Content = NewMessage });
            }

            NewMessage = string.Empty;
        }

        private void UpdateHints()
        {
            if (!string.IsNullOrWhiteSpace(NewMessage) && NewMessage.StartsWith("/"))
            {
                Hints = new ObservableCollection<string>
                {
                    "/currency USD EUR",
                    "/currency EUR GBP",
                    "/disconnect"
                };
                AreHintsVisible = true;
            }
            else
            {
                AreHintsVisible = false;
            }
        }

        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
