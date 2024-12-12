
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using WPFTasks.Core.Models.Currency;
using WPFTasks.ViewModels;

namespace WPFTasks.Core.ViewModels
{
    public class CurrencyClientViewModel : BaseViewModel
    {
        private readonly CurrencyClient _currencyClient;

        private string _ip;
        private int _port;
        private string _login;
        private string _password;
        private string _conversionInput;
        private bool _isAuthenticated;

        public CurrencyClientViewModel()
        {
            _currencyClient = new CurrencyClient();
            _currencyClient.OnAuthenticated += OnAuthenticatedChanged;
            _currencyClient.OnGetErrorMsg += OnErrorReceived;
            _currencyClient.OnGetCurrencyRateMsg += OnCurrencyRateReceived;

            AuthenticateCommand = new RelayCommand(async _ => await AuthenticateAsync(), _ => !IsAuthenticated);
            DisconnectCommand = new RelayCommand(_ => Disconnect(), _ => IsAuthenticated);
            SendConversionCommand = new RelayCommand(async _ => await SendConversionAsync(), _ => IsAuthenticated);

            Messages = new ObservableCollection<string>();
        }

        public string Ip
        {
            get => _ip;
            set => SetProperty(ref _ip, value);
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

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string ConversionInput
        {
            get => _conversionInput;
            set => SetProperty(ref _conversionInput, value);
        }

        public bool IsAuthenticated
        {
            get => _isAuthenticated;
            private set
            {
                if (SetProperty(ref _isAuthenticated, value))
                {
                    // Обновляем доступность кнопок
                    (AuthenticateCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (DisconnectCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (SendConversionCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public ObservableCollection<string> Messages { get; }

        public ICommand AuthenticateCommand { get; }
        public ICommand DisconnectCommand { get; }
        public ICommand SendConversionCommand { get; }

        private async Task AuthenticateAsync()
        {
            try
            {
                await _currencyClient.Init(Ip, Port, Login, Password);
                AddMessage("Client", "Попытка аутентификации...");
            }
            catch (Exception ex)
            {
                AddMessage("Error", $"Ошибка аутентификации: {ex.Message}");
            }
        }

        private void Disconnect()
        {
            _currencyClient.TryDisconnect();

            AddMessage("Client", "Отключение клиента...");
        }

        private async Task SendConversionAsync()
        {
            if (!IsAuthenticated)
            {
                MessageBox.Show("Вы не аутентифицированы. Пожалуйста, войдите в систему.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                var parts = ConversionInput?.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts?.Length != 2)
                {
                    MessageBox.Show("Неправильный формат ввода. Используйте формат: <FROM_CURRENCY> <TO_CURRENCY>", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                await _currencyClient.RequestCurrencyRate(parts[0], parts[1]);
                AddMessage("Client", $"Запрос на конвертацию отправлен: {ConversionInput}");
            }
            catch (Exception ex)
            {
                AddMessage("Error", $"Ошибка отправки запроса: {ex.Message}");
            }
        }

        private void OnAuthenticatedChanged(bool isAuthenticated)
        {
            IsAuthenticated = isAuthenticated;
            AddMessage("Server", isAuthenticated ? "Аутентификация успешна." : "Аутентификация не выполнена.");
        }

        private void OnErrorReceived(string errorMsg)
        {
            AddMessage("Error", errorMsg);
        }

        private void OnCurrencyRateReceived(string fromCurrency, string toCurrency, double rate)
        {
            AddMessage("Server", $"Курс {fromCurrency} -> {toCurrency}: {rate}");
        }

        private void AddMessage(string header, string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Messages.Add($"[{header}] {message}");
            });
        }
    }
}
