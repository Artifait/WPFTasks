
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Windows.Input;
using WPFTasks.Core.Models;
using WPFTasks.Core.Models.Currency;
using WPFTasks.ViewModels;

namespace WPFTasks.Core.ViewModels
{
    public class CurrencyServerViewModel : BaseViewModel
    {
        private static readonly CurrencyServer _server = new();
        private readonly ChatCommandProcessor _commandProcessor = new();

        private CancellationTokenSource _cancellationTokenSource;
        private string _content;
        private string _ipAddress = "127.0.0.1";
        private int _port = 18080;
        private string _newMessage;

        // Состояние
        private bool _areHintsVisible;
        private ObservableCollection<string> _hints = [];

        public CurrencyServerViewModel()
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
                .AddCommand("/OpenUserDataFile", "/OpenUserDataFile", OpenUserDataFileHandler);
            //.AddCommand("SetMaxSessionDuration", "SetMaxSessionDuration", );
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

            try {
                await _commandProcessor.ExecuteCommand(input);
            }
            catch (Exception ex) {
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

        public string TryCompleteCommand(string text)
        {
            if (_commandProcessor.TryCompleteCommand(text, out var completedCommand))
                return completedCommand;

            return text;
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
                .AppendLine($"    MaxSessionDuration: {_server.MaxSessionDuration}")
                .AppendLine($"    FilePathToUserData: {_server.FilePath}")
                .AppendLine("}");

            LogMessage(sb.ToString());
            await Task.CompletedTask;
        }

        // Очистка сообщений
        private void ClearMessages()
            => Content = string.Empty;
        private void UpdateHints()
        {
            int index = NewMessage.LastIndexOf('/');
            if (index == -1) { AreHintsVisible = false; return; }
            string text = NewMessage[index..];
            _commandProcessor.GetHints(text, Hints);
            AreHintsVisible = Hints.Any();
        }

        // Обработчик сообщений лога
        private void OnLogMessage(string message)
            => LogMessage(message);
            
        private void LogMessage(string message)
            => Content += $"{message}\n";
    }
}

