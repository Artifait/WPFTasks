
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
                .AddCommand("/SendMessage", "/SendMessage <login> <message>", SendMessageHandler)
                // Новые команды администратора:
                .AddCommand("/DeleteUser", "/DeleteUser <Login>", DeleteUserHandler)
                .AddCommand("/BanUser", "/BanUser <Login> <hh:mm:ss>", BanUserHandler);
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
            if (parts.Length >= 3)
            {
                try
                {
                    // Собираем сообщение, которое может содержать пробелы
                    string message = input.Substring(input.IndexOf(parts[2]));
                    if (await _server.SendMessageToUser(parts[1], message.TrimEnd()))
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

        // Новый обработчик для удаления пользователя
        private async Task DeleteUserHandler(string input)
        {
            var parts = input.Split(' ');
            if (parts.Length == 2)
            {
                try
                {
                    _server.DeleteUser(parts[1]);
                    LogMessage($"[/DeleteUser]: Пользователь {parts[1]} успешно удалён.");
                }
                catch (Exception ex)
                {
                    LogMessage($"[/DeleteUser]: {ex.Message}");
                }
            }
            else
            {
                LogMessage("[/DeleteUser]: Неверный формат вызова...");
            }
            await Task.CompletedTask;
        }

        // Новый обработчик для бана пользователя
        private async Task BanUserHandler(string input)
        {
            var parts = input.Split(' ');
            if (parts.Length == 3)
            {
                try
                {
                    TimeSpan duration = TimeSpan.Parse(parts[2]);
                    _server.BanUser(parts[1], duration);
                    LogMessage($"[/BanUser]: Пользователь {parts[1]} забанен на {duration}.");
                }
                catch (Exception ex)
                {
                    LogMessage($"[/BanUser]: {ex.Message}");
                }
            }
            else
            {
                LogMessage("[/BanUser]: Неверный формат вызова...");
            }
            await Task.CompletedTask;
        }
    }
}
