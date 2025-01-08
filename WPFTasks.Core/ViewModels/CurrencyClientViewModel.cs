using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using WPFTasks.Core.Models;
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
        private readonly ChatCommandProcessor _commandProcessor;

        // Поля для подключения
        private string _ipAddress = "127.0.0.1";
        private string _port = "18080";
        private string _newMessage;

        // Состояние
        private bool _areHintsVisible;
        private ObservableCollection<string> _hints = new();

        // Чат и сообщения
        public ObservableCollection<ChatMessage> Messages { get; } = new();

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

        public CurrencyClientViewModel()
        {
            _currencyClient = new CurrencyClient();
            _commandProcessor = new();

            _commandProcessor
                .AddCommand("/GetCurrencyRate", "/GetCurrencyRate <FromCurrency> <ToCurrency>", HandleGetCurrencyRate)
                .AddCommand("/Authentication", "/Authentication <Login> <Password>", HandleAuthentication)
                .AddCommand("/SignOut", "/SignOut", HandleSignOut)
                .AddCommand("/Disconnect", "/Disconnect", HandleDisconnect)
                .AddCommand("/Clear", "/Clear", HandleClear)
                .AddCommand("/Connect", "/Connect <Ip> <Port> or /Connect", HandleConnect);

            // Подписываемся на события
            _currencyClient.OnEndSession += data
                => AddMessage("Server", data.Payload);

            _currencyClient.OnConnectionLost += ()
                => AddMessage("Client", "Соединение потеряно.");

            _currencyClient.OnAuthenticationResponse += response
                => AddMessage("ServerResponse", response.Payload);

            _currencyClient.OnCurrencyResponse += response
                => AddMessage("ServerResponse", $"Курс валют: {response.FromCurrency} → {response.ToCurrency}: {response.Rate}");

            _currencyClient.OnErroreOnClient += error
                => AddMessage("Client", $"Ошибка: {error}");

            _currencyClient.OnErroreFromServer += error
                => AddMessage("Server", $"Ошибка: [{error.Payload}].");

            _currencyClient.OnServerOverloaded += ()
                => AddMessage("ServerResponse", "Сервер перегружен.\nНевозможно подключиться, попробуйте позже...");

            // Инициализация команд
            ConnectCommand = new RelayCommand(_ => ConnectAsync(), _ => CanConnect());
            SendMessageCommand = new RelayCommand(async _ => await SendMessageAsync(), _ => CanSendMessage());
        }

        private async Task SendMessageAsync()
        {
            string input = NewMessage.TrimEnd();
            AddMessage("Вы", NewMessage);
            NewMessage = string.Empty;

            try {
                await _commandProcessor.ExecuteCommand(input);
            }
            catch (Exception ex) {
                AddMessage("_commandProcessor", ex.Message);
            }
        }

        private async Task HandleGetCurrencyRate(string input)
        {
            var parts = input.Split(' ');
            if (parts.Length == 3)
            {
                await _currencyClient.SendCurrencyRequest(parts[1], parts[2]);
            }
            else
            {
                ShowMessageBox("Команда /GetCurrencyRate должна быть в формате: { /GetCurrencyRate <FromCurrency> <ToCurrency> }");
            }
        }

        private async Task HandleAuthentication(string input)
        {
            var parts = input.Split(' ');
            if (parts.Length == 3)
            {
                await _currencyClient.SendAuthRequest(parts[1], parts[2]);
            }
            else
            {
                ShowMessageBox("Команда /Authentication должна быть в формате: { /Authentication <Login> <Password> }");
            }
        }

        private async Task HandleSignOut(string input)
            => await _currencyClient.SendCloseSessionRequest();

        private async Task HandleDisconnect(string input)
            => _currencyClient.Disconnect();

        private async Task HandleClear(string input)
            => Application.Current.Dispatcher.Invoke(() => Messages.Clear());

        private async Task HandleConnect(string input)
        {
            var parts = input.Split(' ');
            if (parts.Length == 3)
            {
                await ConnectAsync(parts[1], int.Parse(parts[2]));
            }
            else if (parts.Length == 1)
            {
                await ConnectAsync();
            }
            else
            {
                ShowMessageBox("Команда /Connect должна быть в формате: { /Connect <Ip> <Port> or /Connect }");
            }
        }

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
                await _currencyClient.ConnectAsync(ipAddress, port);

                if (_currencyClient.IsConnected)
                    AddMessage("Server", "Подключение успешно.");
            }
            catch (Exception ex)
            {
                AddMessage("Server", $"Ошибка подключения: {ex.Message}");
            }
        }

        public void SelectHint(string hint)
        {
            if (!string.IsNullOrWhiteSpace(hint))
            {
                NewMessage = hint;
            }
        }
        public string TryCompleteCommand(string text)
        {
            if (_commandProcessor.TryCompleteCommand(text, out var completedCommand))
                return completedCommand;

            return text;
        }

        private void UpdateHints()
        {
            _commandProcessor.GetHints(NewMessage, Hints);
            AreHintsVisible = Hints.Any();
        }

        private void ShowMessageBox(string message)
            => Application.Current.Dispatcher.Invoke(() => MessageBox.Show(message, "Информация", MessageBoxButton.OK, MessageBoxImage.Information));

        private void AddMessage(string sender, string message)
            => Application.Current.Dispatcher.Invoke(() => Messages.Add(new ChatMessage { Sender = sender, Content = message }));

        private bool CanConnect() => !string.IsNullOrWhiteSpace(IpAddress) && int.TryParse(Port, out _) && !_currencyClient.IsConnected;

        private bool CanSendMessage() => !string.IsNullOrWhiteSpace(NewMessage);

        protected void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
