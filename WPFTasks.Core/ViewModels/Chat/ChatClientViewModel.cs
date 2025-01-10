
using System.Windows;
using WPFTasks.Core.Models.Chat;
using WPFTasks.Core.ViewModels.Core;

namespace WPFTasks.Core.ViewModels.Chat
{
    public class ChatClientViewModel : ClientViewModel
    {
        private readonly ChatClient _chatClient;

        public ChatClientViewModel() : base(new ChatClient())
        {
            _chatClient = (ChatClient)Client;

            // Добавляем команды специфичные для ChatClient
            _commandProcessor!
                .AddCommand("/Authentication", "/Authentication <Login> <Password>", HandleAuthentication)
                .AddCommand("/SignOut", "/SignOut", HandleSignOut)
                .AddCommand("/Disconnect", "/Disconnect", HandleDisconnect)
                .AddCommand("/Clear", "/Clear", HandleClear)
                .AddCommand("/Connect", "/Connect <Ip> <Port> or /Connect", HandleConnect)
                .AddCommand("/Delay", "/Delay <milliseconds>", HandleDelay)
                .AddCommand("/SendMessage", "/SendMessage <message>", HandleSendMessage);

            // Подписываемся на события специфичные для PcClient
            _chatClient.OnEndSession += data
                => AddMessage("Server", data.Payload);

            _chatClient.OnAuthenticationResponse += response
                => AddMessage("ServerResponse", response.Payload);

            _chatClient.OnReceivedChatMessage += response =>
            {
                AddMessage("Server", response.Payload);
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

        private async Task HandleSendMessage(string input)
        {
            var parts = input.Split(' ');
            if (parts.Length == 2)
            {
                await _chatClient.SendChatMessage(parts[1].TrimEnd());
            }
            else
            {
                ShowMessageBox("Команда /SendMessage должна быть в формате: { /SendMessage <Message> }");
            }
        }

        private async Task HandleAuthentication(string input)
        {
            var parts = input.Split(' ');
            if (parts.Length == 3)
            {
                await _chatClient.SendAuthRequest(parts[1], parts[2]);
            }
            else
            {
                ShowMessageBox("Команда /Authentication должна быть в формате: { /Authentication <Login> <Password> }");
            }
        }

        private async Task HandleSignOut(string input)
            => await _chatClient.SendCloseSessionRequest();

        private async Task HandleDisconnect(string input)
            => _chatClient.Disconnect();

        private async Task HandleClear(string input)
            => Application.Current.Dispatcher.Invoke(() => Messages.Clear());

        private async Task HandleConnect(string input)
        {
            var parts = input.Split(' ');
            if (parts.Length == 3)
            {
                await _chatClient.ConnectAsync(parts[1], int.Parse(parts[2]));
            }
            else if (parts.Length == 1)
            {
                await _chatClient.ConnectAsync(IpAddress, int.Parse(Port));
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

