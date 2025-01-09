
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Windows.Input;
using WPFTasks.Core.Models.PcStore;
using WPFTasks.ViewModels;

namespace WPFTasks.Core.ViewModels.PcStore
{
    public class PcServerViewModel : HintOnMessageInputBoxBaseVm
    {
        private static readonly PcServer _server = new();

        private CancellationTokenSource _cancellationTokenSource;
        private string _content;
        private string _ipAddress = "127.0.0.1";
        private int _port = 18080;

        public PcServerViewModel()
        {
            _server.Logger.OnLogged += OnLogMessage;
            Content = _server.Logger.Log;

            StartServerCommand = new RelayCommand(async _ => await StartServer(), _ => CanStartServer());
            StopServerCommand = new RelayCommand(async _ => await StopServer(), _ => CanStopServer());
            ClearMessagesCommand = new RelayCommand(_ => ClearMessages());
            SendMessageCommand = new RelayCommand(async _ => await SendMessageAsync(), _ => CanSendMessage());

            _commandProcessor
                .AddCommand("/GetServerStatus", "/GetServerStatus", GetServerStatusHandler)
                .AddCommand("/Clear", "/Clear", async _ => { ClearMessages(); await Task.CompletedTask; })
                .AddCommand("/OpenUserDataFile", "/OpenUserDataFile", OpenUserDataFileHandler)
                .AddCommand("/RegisterUser", "/RegisterUser <Login> <Password>", RegisterUserHandler)
                .AddCommand("/SetAuthSessionDuration", "/SetAuthSessionDuration <hh:mm:ss>", SetAuthSessionDurationHandler)
                .AddCommand("/SetCountMaxConnections", "/SetCountMaxConnections <count>", SetCountMaxConnectionsHandler)
                .AddCommand("/SetCountMaxRequests", "/SetCountMaxRequests <login> <count>", SetCountMaxRequestsHandler)
                .AddCommand("/SetTimeWindow", "/SetTimeWindow <hh:mm:ss> - настройка промежутка времени, в котором учитываються запросы.", SetTimeWindowHandler);
        }
        private bool CanSendMessage() => !string.IsNullOrWhiteSpace(NewMessage);

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
        public ICommand SendMessageCommand { get; }


        private async Task SendMessageAsync()
        {
            string input = NewMessage.TrimEnd();
            LogMessage("[Root]: " + NewMessage);
            NewMessage = string.Empty;

            try
            {
                await _commandProcessor.ExecuteCommand(input);
            }
            catch (Exception ex)
            {
                LogMessage("[CommandProcessor]: " + ex.Message);
            }
        }

        // Запуск сервера
        private bool CanStartServer() => _cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested;
        private async Task StartServer()
        {
            try
            {
                if (!CanStartServer())
                {
                    LogMessage($"[Server]: Я уже запущен...");
                    return;
                }

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
                if (!CanStopServer())
                {
                    LogMessage($"[Server]: Запусти сначало...");
                    return;
                }
                _cancellationTokenSource?.Cancel();
                await _server.StopServer();
            }
            catch (Exception ex)
            {
                LogMessage($"[Server]: Ошибка остановки сервера: {ex.Message}");
            }
        }

        private async Task OpenUserDataFileHandler(string _)
        {
            if (!System.IO.File.Exists(_server.FilePath))
            {
                LogMessage("[/OpenUserDataFile]: Файл не найден.");
                return;
            }

            Process.Start("notepad.exe", _server.FilePath);
            await Task.CompletedTask;
        }

        private async Task GetServerStatusHandler(string _)
        {
            StringBuilder sb = new();
            sb
                .AppendLine("<ServerStatus>")
                .AppendLine("{")
                .AppendLine($"    IsRunning: {_server.IsRunning}")
                .AppendLine($"    NowConnections: {_server.CountOpenSessions}/{_server.MaxConnections}")
                .AppendLine($"    MaxAuthSessionDuration: {_server.MaxAuthSessionDuration.TotalMinutes} Мин.")
                .AppendLine($"    MaxDurationInactive: {_server.MaxDurationInactive.TotalMinutes} Мин.")
                .AppendLine($"    TimeWindow: {_server.TimeWindow.TotalMinutes}  Мин.")
                .AppendLine($"    FilePathToUserData: {_server.FilePath}")
                .AppendLine("}");

            LogMessage(sb.ToString());
            await Task.CompletedTask;
        }

        private async Task SetAuthSessionDurationHandler(string input)
        {
            var parts = input.Split(' ');
            if (parts.Length == 2)
            {
                try {
                    await _server.UpdateAuthSessionDuration(TimeSpan.Parse(parts[1]));
                }
                catch (Exception ex) {
                    LogMessage($"[/SetAuthSessionDuration]: {ex.Message}");
                }
            }
            else {
                LogMessage("[/SetAuthSessionDuration]: Неверный формат вызова...");
            }
        }

        private async Task SetMaxDurationInactiveHandler(string input)
        {
            var parts = input.Split(' ');
            if (parts.Length == 2)
            {
                try {
                    await _server.UpdateMaxDurationInactive(TimeSpan.Parse(parts[1]));
                }
                catch (Exception ex) {
                    LogMessage($"[/SetMaxDurationInactive]: {ex.Message}");
                }
            }
            else {
                LogMessage("[/SetMaxDurationInactive]: Неверный формат вызова...");
            }
        }

        private async Task SetCountMaxConnectionsHandler(string input)
        {
            var parts = input.Split(' ');
            if (parts.Length == 2)
            {
                try
                {
                    _server.MaxConnections = int.Parse(parts[1]);
                }
                catch (Exception ex)
                {
                    LogMessage($"[/SetSessionDuration]: {ex.Message}");
                }
            }
            else
            {
                LogMessage("[/SetSessionDuration]: Неверный формат вызова...");
            }
            await Task.CompletedTask;
        }

        private async Task SetCountMaxRequestsHandler(string input)
        {
            var parts = input.Split(' ');
            if (parts.Length == 3)
            {
                try {
                    _server.SetIndividMaxRequests(parts[1], int.Parse(parts[2]));
                }
                catch (Exception ex) {
                    LogMessage($"[/SetCountMaxRequests]: {ex.Message}");
                }
            }
            else {
                LogMessage("[/SetCountMaxRequests]: Неверный формат вызова...");
            }
            await Task.CompletedTask;
        }

        private async Task SetTimeWindowHandler(string input)
        {
            var parts = input.Split(' ');
            if (parts.Length == 2)
            {
                try {
                    _server.TimeWindow = TimeSpan.Parse(parts[1]);
                }
                catch (Exception ex) {
                    LogMessage($"[/SetTimeWindow]: {ex.Message}");
                }
            }
            else {
                LogMessage("[/SetTimeWindow]: Неверный формат вызова...");
            }
            await Task.CompletedTask;
        }

        private async Task RegisterUserHandler(string input)
        {
            var parts = input.Split(' ');
            if (parts.Length == 3)
            {
                try {
                    _server.RegisterUser(parts[1], parts[2]);
                    LogMessage($"[/RegisterUser]: успешно добавлен новый пользователь под логином: {parts[1]}.");
                }
                catch (Exception ex) {
                    LogMessage($"[/RegisterUser]: {ex.Message}");
                }
            }
            else {
                LogMessage("[/RegisterUser]: Неверный формат вызова...");
            }
            await Task.CompletedTask;
        }
        // Очистка сообщений
        private void ClearMessages()
            => Content = string.Empty;

        // Обработчик сообщений лога
        private void OnLogMessage(string message)
            => LogMessage(message);

        private void LogMessage(string message)
            => Content += $"[{DateTime.UtcNow.ToString("HH:mm:ss.ffff")}]{message}\n";
    }
}
