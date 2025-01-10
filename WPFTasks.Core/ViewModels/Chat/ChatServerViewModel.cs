
using System.Diagnostics;
using System.Net;
using System.Text;
using WPFTasks.Core.Models.Chat;
using WPFTasks.Core.ViewModels.Core;

namespace WPFTasks.Core.ViewModels.Chat
{
    public class ChatServerViewModel : ServerViewModel
    {
        private static readonly ChatServer _server = new();

        public ChatServerViewModel()
        {
            _server.Logger.OnLogged += OnLogMessage;
            _server.OnReceivedMessageFromUser += (user, message) => LogMessage($"[{user.Login}]: {message.Payload}");

            _commandProcessor
                .AddCommand("/GetServerStatus", "/GetServerStatus", GetServerStatusHandler)
                .AddCommand("/Clear", "/Clear", async _ => ClearMessages())
                .AddCommand("/OpenUserDataFile", "/OpenUserDataFile", OpenUserDataFileHandler)
                .AddCommand("/RegisterUser", "/RegisterUser <Login> <Password>", RegisterUserHandler)
                .AddCommand("/SetAuthSessionDuration", "/SetAuthSessionDuration <hh:mm:ss>", SetAuthSessionDurationHandler)
                .AddCommand("/SetMaxDurationInactive", "/SetMaxDurationInactive <hh:mm:ss>", SetMaxDurationInactiveHandler)
                .AddCommand("/SetCountMaxConnections", "/SetCountMaxConnections <count>", SetCountMaxConnectionsHandler)
                .AddCommand("/SendMessage", "/ SendMessage <login> <message>", SendMessageHandler);
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


        private async Task SendMessageHandler(string input)
        {
            var parts = input.Split(' ');
            if (parts.Length == 3)
            {
                try
                {
                    if(await _server.SendMessageToUser(parts[1], parts[2].TrimEnd()))
                    {
                        LogMessage($"[/SendMessage]: Сообщение успешно отправлено");
                    }
                    else
                    {
                        LogMessage($"[/SendMessage]: Не нашли активного юзера под логином - {parts[1]}");
                    }
                }
                catch (Exception ex)
                {
                    LogMessage($"[/SendMessage]: {ex.Message}");
                }
            }
            else
            {
                LogMessage("[/SendMessage]: Неверный формат вызова...");
            }
        }

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
                try
                {
                    await _server.UpdateAuthSessionDuration(TimeSpan.Parse(parts[1]));
                }
                catch (Exception ex)
                {
                    LogMessage($"[/SetAuthSessionDuration]: {ex.Message}");
                }
            }
            else
            {
                LogMessage("[/SetAuthSessionDuration]: Неверный формат вызова...");
            }
        }

        private async Task SetMaxDurationInactiveHandler(string input)
        {
            var parts = input.Split(' ');
            if (parts.Length == 2)
            {
                try
                {
                    await _server.UpdateMaxDurationInactive(TimeSpan.Parse(parts[1]));
                }
                catch (Exception ex)
                {
                    LogMessage($"[/SetMaxDurationInactive]: {ex.Message}");
                }
            }
            else
            {
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

        private async Task RegisterUserHandler(string input)
        {
            var parts = input.Split(' ');
            if (parts.Length == 3)
            {
                try
                {
                    _server.RegisterUser(parts[1], parts[2]);
                    LogMessage($"[/RegisterUser]: успешно добавлен новый пользователь под логином: {parts[1]}.");
                }
                catch (Exception ex)
                {
                    LogMessage($"[/RegisterUser]: {ex.Message}");
                }
            }
            else
            {
                LogMessage("[/RegisterUser]: Неверный формат вызова...");
            }
            await Task.CompletedTask;
        }
    }
}
