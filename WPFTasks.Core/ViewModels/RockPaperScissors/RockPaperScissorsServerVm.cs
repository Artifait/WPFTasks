using System.Net;
using System.Text;
using WPFTasks.Core.Models.RockPaperScissors;
using WPFTasks.Core.ViewModels.Core;

namespace WPFTasks.Core.ViewModels.RockPaperScissors
{
    public class RockPaperScissorsServerVm : ServerViewModel
    {
        private static readonly RockPaperScissorsServer _server = new();

        public RockPaperScissorsServerVm()
        {
            _server.Logger.OnLogged += OnLogMessage;

            _commandProcessor
                .AddCommand("/GetServerStatus", "/GetServerStatus", GetServerStatusHandler)
                .AddCommand("/Clear", "/Clear", async _ => ClearMessages())
                .AddCommand("/SetMaxDurationInactive", "/SetMaxDurationInactive <hh:mm:ss>", SetMaxDurationInactiveHandler)
                .AddCommand("/SetCountMaxConnections", "/SetCountMaxConnections <count>", SetCountMaxConnectionsHandler);
        }

        private async Task GetServerStatusHandler(string _)
        {
            StringBuilder sb = new();
            sb.AppendLine("<ServerStatus>")
              .AppendLine("{")
              .AppendLine($"    IsRunning: {_server.IsRunning}")
              .AppendLine($"    NowConnections: {_server.CountOpenSessions}/{_server.MaxConnections}")
              .AppendLine($"    MaxDurationInactive: {_server.MaxDurationInactive.TotalMinutes} Мин.")
              .AppendLine("}");
            LogMessage(sb.ToString());
            await Task.CompletedTask;
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

        protected override async Task StartServerImplementationAsync()
        {
            _server.SetEndPoint(new IPEndPoint(System.Net.IPAddress.Parse(IpAddress), Port));
            await _server.StartServer(_cancellationTokenSource.Token);
        }

        protected override async Task StopServerImplementationAsync() => await _server.StopServer();

        protected override async Task ProcessCommandAsync(string command) => await _commandProcessor.ExecuteCommand(command);

        private void OnLogMessage(string message) => LogMessage(message);
    }
}
