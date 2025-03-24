
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

            // Регистрируем команды, специфичные для ChatClient
            _commandProcessor!
                .AddCommand("/Authentication", "/Authentication <Login> <Password>", HandleAuthentication)
                .AddCommand("/SignOut", "/SignOut", HandleSignOut)
                .AddCommand("/Disconnect", "/Disconnect", HandleDisconnect)
                .AddCommand("/Clear", "/Clear", HandleClear)
                .AddCommand("/Connect", "/Connect <Ip> <Port> or /Connect", HandleConnect)
                .AddCommand("/Delay", "/Delay <milliseconds>", HandleDelay)
                // Теперь команда ожидает: /SendMessage <chatId> <Message>
                .AddCommand("/SendMessage", "/SendMessage <chatId> <Message>", HandleSendMessage);

            // Подписываемся на события от ChatClient
            _chatClient.OnEndSession += data =>
                AddMessage("Server", data.Payload);

            _chatClient.OnAuthenticationResponse += response =>
                AddMessage("ServerResponse", response.Payload);

            _chatClient.OnChatUpdated += response =>
            {
                AddMessage($"{response.Sender}:(Room: {response.ChatId})", response.Content);
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
            // Формат: /SendMessage <chatId> <message>
            var parts = input.Split(' ');
            if (parts.Length >= 3)
            {
                // chatId - второй параметр, остальное – сообщение (может содержать пробелы)
                string chatId = parts[1];
                string message = input.Substring(input.IndexOf(parts[2]));
                await _chatClient.SendChatMessage(message.TrimEnd(), chatId);
            }
            else
            {
                ShowMessageBox("Команда /SendMessage должна быть в формате: { /SendMessage <chatId> <Message> }");
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
            var parts = input.Split(' ');
            if (parts.Length == 2 && int.TryParse(parts[1], out int milliseconds))
            {
                await Task.Delay(milliseconds);
            }
            else
            {
                ShowMessageBox("Команда /Delay должна быть в формате: { /Delay <milliseconds> }");
                throw new ArgumentException("Неверный формат /Delay...");
            }
        }
    }
}

