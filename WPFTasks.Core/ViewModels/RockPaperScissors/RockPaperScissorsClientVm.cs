using System.Threading.Tasks;
using WPFTasks.Core.Models.RockPaperScissors;
using WPFTasks.Core.ViewModels.Core;
using WPFTasks.Core.Models.RockPaperScissors.MessageBuilder;
using WPFTasks.Core.Models.RockPaperScissors.Services.RockPaperScissorsLogic;

namespace WPFTasks.Core.ViewModels.RockPaperScissors
{
    public class RockPaperScissorsClientVm : ClientViewModel
    {
        private readonly RockPaperScissorsClient _client;
        private string _lastPrompt = "Нет информации...";
        private string _lastInstructions = "Нет информации...";

        public RockPaperScissorsClientVm() : base(new RockPaperScissorsClient())
        {
            _client = (RockPaperScissorsClient)Client;

            _commandProcessor!
                .AddCommand("/Disconnect", "/Disconnect", HandleDisconnect)
                .AddCommand("/Clear", "/Clear", HandleClear)
                .AddCommand("/Connect", "/Connect <Ip: str> <Port: int> or /Connect", HandleConnect)
                .AddCommand("/Delay", "/Delay <milliseconds: int>", HandleDelay)
                .AddCommand("/FindGame", "/FindGame <Type: int> 0 -> Human-Human; 1 -> Human-Pc; 2 -> Pc-Pc", HandleFindGame)
                .AddCommand("/SendMove", "/SendMove <Move: int> (0 -> Rock; 1 -> Paper; 2 -> Scissors)", HandleSendMove)
                .AddCommand("/GiveUp", "/GiveUp - Сдаться", HandleGiveUp)
                .AddCommand("/SuggestDraw", "/SuggestDraw - Предложить ничью", HandleSuggestTie)
                .AddCommand("/GetInstructions", "/GetInstructions - Показать инструкции", async _ => AddMessage("Save", _lastInstructions));

            _client.OnFindGameResponse += data => AddMessage("Server", data.SearchState);
            _client.OnEndSession += data => AddMessage("Server", data.Payload);
            _client.OnGameEnded += data => AddMessage("Server", $"Игра завершена!\nРезультат: {data.GameResult}\nХоды: {data.Player1Move} и {data.Player2Move}");
            _client.OnTieOffered += () => AddMessage("Server", "Сервер: Второй игрок предлагает ничью. Для согласия используйте /SuggestDraw");
            _client.OnGameStarted += data =>
            {
                _lastInstructions = data.Instructions;
                AddMessage("Server", $"Игра началась!\n{data.Instructions}");
            };
            _client.OnPlayerTurn += data =>
            {
                _lastPrompt = data.Prompt;
                AddMessage("Server", $"{data.Prompt}");
            };
            _client.OnRoundResult += data =>
            {
                AddMessage("Server", $"Результат раунда: {data.Result}");
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

        private async Task HandleFindGame(string input)
        {
            var parts = input.Split(" ");
            if (parts.Length == 2)
            {
                await _client.SendFindGameRequest((GameTypes)int.Parse(parts[1]));
            }
            else
            {
                ShowMessageBox("Неверный формат /FindGame...");
                throw new ArgumentException("Неверный формат /FindGame...");
            }
        }

        private async Task HandleSendMove(string input)
        {
            var parts = input.Split(" ");
            if (parts.Length == 2)
            {
                await _client.SendUserMove(int.Parse(parts[1]));
            }
            else
            {
                ShowMessageBox("Неверный формат /SendMove...");
                throw new ArgumentException("Неверный формат /SendMove...");
            }
        }

        private async Task HandleGiveUp(string _) => await _client.SendGiveUp();
        private async Task HandleSuggestTie(string _) => await _client.SendTieRequest();
        private async Task HandleDisconnect(string input) => _client.Disconnect();
        private async Task HandleClear(string input) => System.Windows.Application.Current.Dispatcher.Invoke(() => Messages.Clear());
        private async Task HandleConnect(string input)
        {
            var parts = input.Split(' ');
            if (parts.Length == 3)
            {
                await _client.ConnectAsync(parts[1], int.Parse(parts[2]));
            }
            else if (parts.Length == 1)
            {
                await _client.ConnectAsync(IpAddress, int.Parse(Port));
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
