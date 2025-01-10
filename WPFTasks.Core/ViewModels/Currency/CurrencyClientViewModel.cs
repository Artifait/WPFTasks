
using System.Collections.ObjectModel;
using WPFTasks.Core.Models.Currency;
using System.Windows.Input;
using System.Windows;
using WPFTasks.Core.ViewModels.Core;

namespace WPFTasks.Core.ViewModels.Currency
{
    public class CurrencyClientViewModel : HintOnMessageInputBoxBaseVm
    {
        private readonly CurrencyClient _currencyClient;

        // Поля для подключения
        private string _ipAddress = "127.0.0.1";
        private string _port = "18080";

        // Чат и сообщения
        public ObservableCollection<ChatMessage> Messages { get; } = new();

        // Свойства
        public string IpAddress
        {
            get => _ipAddress;
            set => SetProperty(ref _ipAddress, value);
        }

        public string Port
        {
            get => _port;
            set => SetProperty<string>(ref _port, value);
        }

        // Команды
        public ICommand ConnectCommand { get; }
        public ICommand SendMessageCommand { get; }
        public event Action? OnUpdateMessages;

        public CurrencyClientViewModel() : base()
        {
            _currencyClient = new CurrencyClient();

            _commandProcessor!
                .AddCommand("/GetCurrencyRate", "/GetCurrencyRate <FromCurrency> <ToCurrency>", HandleGetCurrencyRate)
                .AddCommand("/Authentication", "/Authentication <Login> <Password>", HandleAuthentication)
                .AddCommand("/SignOut", "/SignOut", HandleSignOut)
                .AddCommand("/Disconnect", "/Disconnect", HandleDisconnect)
                .AddCommand("/Clear", "/Clear", HandleClear)
                .AddCommand("/Connect", "/Connect <Ip> <Port> or /Connect", HandleConnect)
                .AddCommand("/Delay", "/Delay <milliseconds>", HandleDelay);

            // Подписываемся на события
            _currencyClient.OnEndSession += data
                => AddMessage("Server", data.Payload);

            _currencyClient.OnConnectionLost += ()
                => AddMessage("Client", "Соединение потеряно.");

            _currencyClient.OnAuthenticationResponse += response
                => AddMessage("ServerResponse", response.Payload);

            _currencyClient.OnCurrencyResponse += response =>
            {
                AddMessage("ServerResponse", $"Курс валют: {response.FromCurrency} → {response.ToCurrency}: " +
                    (response.Rate == -1 ? "Не найдено..." : response.Rate.ToString()));
            };

            _currencyClient.OnErroreOnClient += error
                => AddMessage("Client", $"Ошибка: {error}");

            _currencyClient.OnErroreFromServer += error
                => AddMessage("Server", $"Ошибка: [{error.Payload}].");

            _currencyClient.OnServerOverloaded += ()
                => AddMessage("ServerResponse", "Сервер перегружен.\nНевозможно подключиться, попробуйте позже...");

            // Инициализация команд
            ConnectCommand = new RelayCommand(async _ => await ConnectAsync(), _ => CanConnect());
            SendMessageCommand = new RelayCommand(async _ => await SendMessageAsync(), _ => CanSendMessage());
        }

        private async Task SendMessageAsync()
        {
            string input = NewMessage.TrimEnd();
            AddMessage("Вы", NewMessage);
            NewMessage = string.Empty;

            try
            {
                await _commandProcessor.ExecuteCommand(input);
            }
            catch (Exception ex)
            {
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

        private async Task HandleDelay(string input)
        {
            var parts = input.Split(" ");
            if (parts.Length == 2)
                await Task.Delay(int.Parse(parts[1]));
            else
            {
                ShowMessageBox("Команда /Delay должна быть в формате: { /Delay <milliseconds> }");
                throw new ArgumentException("Неверный формат /Delay...");
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

        private void ShowMessageBox(string message)
            => Application.Current.Dispatcher.Invoke(() => MessageBox.Show(message, "Информация", MessageBoxButton.OK, MessageBoxImage.Information));

        private void AddMessage(string sender, string message)
            => Application.Current.Dispatcher.Invoke(() => { Messages.Add(new ChatMessage { Sender = sender, Content = message }); OnUpdateMessages?.Invoke(); });

        private bool CanConnect() => !string.IsNullOrWhiteSpace(IpAddress) && int.TryParse(Port, out _) && !_currencyClient.IsConnected;

        private bool CanSendMessage() => !string.IsNullOrWhiteSpace(NewMessage);
    }
}
