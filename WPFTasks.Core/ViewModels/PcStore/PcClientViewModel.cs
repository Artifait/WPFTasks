
using System.Windows;
using WPFTasks.Core.Models.PcStore;
using WPFTasks.Core.ViewModels.Core;

namespace WPFTasks.Core.ViewModels.PcStore
{
    public class PcClientViewModel : ClientViewModel
    {
        private readonly PcClient _pcClient;

        public PcClientViewModel() : base(new PcClient())
        {
            _pcClient = (PcClient)Client;

            // Добавляем команды специфичные для PcClient
            _commandProcessor!
                .AddCommand("/GetPcPartPrice", "/GetPcPartPrice <PcPart>", HandleGetPcPartPrice)
                .AddCommand("/Authentication", "/Authentication <Login> <Password>", HandleAuthentication)
                .AddCommand("/SignOut", "/SignOut", HandleSignOut)
                .AddCommand("/Disconnect", "/Disconnect", HandleDisconnect)
                .AddCommand("/Clear", "/Clear", HandleClear)
                .AddCommand("/Connect", "/Connect <Ip> <Port> or /Connect", HandleConnect)
                .AddCommand("/Delay", "/Delay <milliseconds>", HandleDelay);

            // Подписываемся на события специфичные для PcClient
            _pcClient.OnEndSession += data
                => AddMessage("Server", data.Payload);

            _pcClient.OnAuthenticationResponse += response
                => AddMessage("ServerResponse", response.Payload);

            _pcClient.OnPcPartInfoResponse += response =>
            {
                AddMessage("ServerResponse", response.Price == -1 ? response.Title : $"{response.Title} - {response.Price} $");
            };
        }

        protected override async Task ExecuteCommandAsync(string input)
        {
            try
            {
                await _commandProcessor.ExecuteCommand(input);
            }
            catch (Exception ex)
            {
                AddMessage("_commandProcessor", ex.Message);
            }
        }

        private async Task HandleGetPcPartPrice(string input)
        {
            var parts = input.Split(' ');
            if (parts.Length == 2)
            {
                await _pcClient.SendPcPartInfoRequest(parts[1]);
            }
            else
            {
                ShowMessageBox("Команда /GetPcPartPrice должна быть в формате: { /GetPcPartPrice <TitleOfPart> }");
            }
        }

        private async Task HandleAuthentication(string input)
        {
            var parts = input.Split(' ');
            if (parts.Length == 3)
            {
                await _pcClient.SendAuthRequest(parts[1], parts[2]);
            }
            else
            {
                ShowMessageBox("Команда /Authentication должна быть в формате: { /Authentication <Login> <Password> }");
            }
        }

        private async Task HandleSignOut(string input)
            => await _pcClient.SendCloseSessionRequest();

        private async Task HandleDisconnect(string input)
            => _pcClient.Disconnect();

        private async Task HandleClear(string input)
            => Application.Current.Dispatcher.Invoke(() => Messages.Clear());

        private async Task HandleConnect(string input)
        {
            var parts = input.Split(' ');
            if (parts.Length == 3)
            {
                await _pcClient.ConnectAsync(parts[1], int.Parse(parts[2]));
            }
            else if (parts.Length == 1)
            {
                await _pcClient.ConnectAsync(IpAddress, int.Parse(Port));
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
            {
                await Task.Delay(int.Parse(parts[1]));
            }
            else
            {
                ShowMessageBox("Команда /Delay должна быть в формате: { /Delay <milliseconds> }");
                throw new ArgumentException("Неверный формат /Delay...");
            }
        }
    }
}
