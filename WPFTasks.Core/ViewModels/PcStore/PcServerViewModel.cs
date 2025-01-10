
using System.Diagnostics;
using System.Net;
using System.Text;
using WPFTasks.Core.Models.PcStore;
using WPFTasks.Core.ViewModels.Core;

namespace WPFTasks.Core.ViewModels.PcStore
{
    public class PcServerViewModel : ServerViewModel
    {
        private static readonly PcServer _server = new();

        public PcServerViewModel()
        {
            _server.Logger.OnLogged += OnLogMessage;

            _commandProcessor
                .AddCommand("/GetServerStatus", "/GetServerStatus", GetServerStatusHandler)
                .AddCommand("/Clear", "/Clear", async _ => ClearMessages())
                .AddCommand("/OpenUserDataFile", "/OpenUserDataFile", OpenUserDataFileHandler)
                .AddCommand("/OpenPcPartDataFile", "/OpenPcPartDataFile", OpenPcPartFileHandler)
                .AddCommand("/RegisterUser", "/RegisterUser <Login> <Password>", RegisterUserHandler)
                .AddCommand("/SetAuthSessionDuration", "/SetAuthSessionDuration <hh:mm:ss>", SetAuthSessionDurationHandler)
                .AddCommand("/SetMaxDurationInactive", "/SetMaxDurationInactive <hh:mm:ss>", SetMaxDurationInactiveHandler)
                .AddCommand("/SetCountMaxConnections", "/SetCountMaxConnections <count>", SetCountMaxConnectionsHandler)
                .AddCommand("/SetCountMaxRequests", "/SetCountMaxRequests <login> <count>", SetCountMaxRequestsHandler)
                .AddCommand("/SetTimeWindow", "/SetTimeWindow <login> <hh:mm:ss>", SetTimeWindowHandler);
        }

        protected override async Task StartServerImplementationAsync()
        {
            _server.SetEndPoint(new IPEndPoint(IPAddress.Parse(IpAddress), Port));
            await _server.StartServer(_cancellationTokenSource.Token);
        }

        protected override async Task StopServerImplementationAsync()
            => await _server.StopServer();

        protected override async Task ProcessCommandAsync(string command)
            => await _commandProcessor.ExecuteCommand(command);

        private void OnLogMessage(string message) 
            => LogMessage(message);

        private async Task OpenUserDataFileHandler(string _)
        {
            if (!System.IO.File.Exists(_server.UserFilePath))
            {
                LogMessage("[/OpenUserDataFile]: Файл не найден.");
                return;
            }

            Process.Start("notepad.exe", _server.UserFilePath);
            await Task.CompletedTask;
        }

        private async Task OpenPcPartFileHandler(string _)
        {
            if (!System.IO.File.Exists(_server.PcPartFilePath))
            {
                LogMessage("[/OpenPcPartFile]: Файл не найден.");
                return;
            }

            Process.Start("notepad.exe", _server.PcPartFilePath);
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
                .AppendLine($"    FilePathToUserData: {_server.UserFilePath}")
                .AppendLine($"    FilePathToPcPartData: {_server.UserFilePath}")
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
                    LogMessage($"[/SetCountMaxConnections]: {ex.Message}");
                }
            }
            else
            {
                LogMessage("[/SetCountMaxConnections]: Неверный формат вызова...");
            }
            await Task.CompletedTask;
        }

        private async Task SetCountMaxRequestsHandler(string input)
        {
            var parts = input.Split(' ');
            if (parts.Length == 3)
            {
                try {
                    if(_server.SetIndividMaxRequests(parts[1], int.Parse(parts[2]))) {
                        LogMessage($"[/SetCountMaxRequests]: Успешно обновлён лимит запросов для {parts[1]}");
                    }
                    else {
                        LogMessage($"[/SetCountMaxRequests]: Не нашли пользователя под логином {parts[1]}");
                    }
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
            if (parts.Length == 3)
            {
                try {
                    if(_server.SetIndividTimeWindow(parts[1], TimeSpan.Parse(parts[2]))) {
                        LogMessage($"[/SetTimeWindow]: Успешно обновлено временое окно для {parts[1]}");
                    }
                    else {
                        LogMessage($"[/SetTimeWindow]: Не нашли пользователя под логином {parts[1]}");
                    }
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
    }
}
