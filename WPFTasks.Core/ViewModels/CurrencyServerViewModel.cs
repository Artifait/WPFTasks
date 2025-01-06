
using System.Net;
using System.Windows.Input;
using WPFTasks.Core.Models.Currency;
using WPFTasks.ViewModels;

namespace WPFTasks.Core.ViewModels
{
    public class CurrencyServerViewModel : BaseViewModel
    {
        private static readonly CurrencyServer _server = new();
        private CancellationTokenSource _cancellationTokenSource;
        private string _content;
        private string _ipAddress = "127.0.0.1";
        private int _port = 18080;

        public CurrencyServerViewModel()
        {
            _server.Logger.OnLogged += OnLogMessage;
            Content = _server.Logger.Log;

            StartServerCommand = new RelayCommand(async _ => await StartServer(), _ => CanStartServer());
            StopServerCommand = new RelayCommand(async _ => await StopServer(), _ => CanStopServer());
            ClearMessagesCommand = new RelayCommand(_ => ClearMessages());
        }

        // Привязка консоли
        public string Content
        {
            get => _content;
            set => SetProperty(ref _content, value);
        }

        // IP-адрес
        public string IpAddress
        {
            get => _ipAddress;
            set => SetProperty(ref _ipAddress, value);
        }

        // Порт
        public int Port
        {
            get => _port;
            set => SetProperty(ref _port, value);
        }

        // Команды
        public ICommand StartServerCommand { get; }
        public ICommand StopServerCommand { get; }
        public ICommand ClearMessagesCommand { get; }

        // Запуск сервера
        private bool CanStartServer() => _cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested;
        private async Task StartServer()
        {
            try
            {
                _server.SetEndPoint(new IPEndPoint(IPAddress.Parse(IpAddress), Port));
                _cancellationTokenSource = new CancellationTokenSource();
                await _server.StartServer(_cancellationTokenSource.Token);
                
                //LogMessage("[Server]: Сервер запущен.");
            }
            catch (Exception ex)
            {
                LogMessage($"[Server]: Ошибка запуска сервера: {ex.Message}");
            }
        }

        // Остановка сервера
        private bool CanStopServer() => _cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested;
        private async Task StopServer()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                await _server.StopServer();
            }
            catch (Exception ex)
            {
                LogMessage($"[Server]: Ошибка остановки сервера: {ex.Message}");
            }
        }

        // Очистка сообщений
        private void ClearMessages()
            => Content = string.Empty;

        // Обработчик сообщений лога
        private void OnLogMessage(string message)
            => LogMessage(message);
            
        private void LogMessage(string message)
            => Content += $"{message}\n";
    }
}

