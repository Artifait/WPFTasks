
using System.Windows.Input;

namespace WPFTasks.Core.ViewModels.Core
{
    public abstract class ServerViewModel : HintOnMessageInputBoxBaseVm
    {
        private string _content;
        private string _ipAddress = "127.0.0.1";
        private int _port = 18080;

        protected CancellationTokenSource _cancellationTokenSource;

        public ServerViewModel()
        {
            StartServerCommand = new RelayCommand(async _ => await StartServerAsync(), _ => CanStartServer());
            StopServerCommand = new RelayCommand(async _ => await StopServerAsync(), _ => CanStopServer());
            ClearMessagesCommand = new RelayCommand(_ => ClearMessages());
            SendMessageCommand = new RelayCommand(async _ => await SendMessageAsync(), _ => CanSendMessage());
        }

        public string Content
        {
            get => _content;
            set => SetProperty(ref _content, value);
        }

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

        public ICommand StartServerCommand { get; }
        public ICommand StopServerCommand { get; }
        public ICommand ClearMessagesCommand { get; }
        public ICommand SendMessageCommand { get; }

        protected abstract Task StartServerImplementationAsync();
        protected abstract Task StopServerImplementationAsync();
        protected abstract Task ProcessCommandAsync(string command);

        private bool CanStartServer() => _cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested;

        private async Task StartServerAsync()
        {
            if (!CanStartServer())
            {
                LogMessage($"[Server]: Сервер уже запущен.");
                return;
            }

            try
            {
                _cancellationTokenSource = new CancellationTokenSource();
                await StartServerImplementationAsync();
            }
            catch (Exception ex)
            {
                LogMessage($"[Server]: Ошибка запуска: {ex.Message}");
            }
        }

        private bool CanStopServer() => _cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested;

        private async Task StopServerAsync()
        {
            if (!CanStopServer())
            {
                LogMessage($"[Server]: Сервер не запущен.");
                return;
            }

            try
            {
                _cancellationTokenSource.Cancel();
                await StopServerImplementationAsync();
            }
            catch (Exception ex)
            {
                LogMessage($"[Server]: Ошибка остановки: {ex.Message}");
            }
        }

        private bool CanSendMessage() => !string.IsNullOrWhiteSpace(NewMessage);

        private async Task SendMessageAsync()
        {
            string command = NewMessage.TrimEnd();
            LogMessage($"[Input]: {command}");
            NewMessage = string.Empty;

            try
            {
                await ProcessCommandAsync(command);
            }
            catch (Exception ex)
            {
                LogMessage($"[Error]: {ex.Message}");
            }
        }

        protected void ClearMessages() => Content = string.Empty;

        protected void LogMessage(string message)
        {
            Content += $"[{DateTime.UtcNow:HH:mm:ss.ffff}] {message}\n";
        }
    }

}
