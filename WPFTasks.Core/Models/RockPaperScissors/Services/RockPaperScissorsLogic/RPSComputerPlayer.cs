
using TopNetwork.RequestResponse;

namespace WPFTasks.Core.Models.RockPaperScissors.Services.RockPaperScissorsLogic
{
    public class ComputerPlayer : BasePlayer
    {
        private static readonly Random _random = new();
        private bool _hasRespondedToTieRequest = false;
        private readonly LogString? _logger;

        public ComputerPlayer(string identifier, LogString? logger = null)
        {
            Identifier = identifier;
            _logger = logger;
            Name = identifier;
        }

        public string Identifier { get; private set; }

        public override async Task OnPlayerTurn()
        {
            // Симуляция задержки "размышления"
            await Task.Delay(_random.Next(500, 1500));
            int move = _random.Next(0, 3); // 0 - Rock, 1 - Paper, 2 - Scissors
            InvokeOnGetPlayerMove(move);
        }

        public override async Task OnResultTurn(string result)
        {
            // Дополнительные действия можно добавить при необходимости
            await Task.CompletedTask;
        }

        public override async Task OnTieOffered()
        {
            if (_hasRespondedToTieRequest)
            {
                _logger?.Invoke($"[ComputerPlayer]: Игнорирую повторное предложение ничьей.");
                return;
            }

            _hasRespondedToTieRequest = true;
            bool acceptTie = _random.Next(0, 100) < 30; // 30% шанс принять ничью

            if (acceptTie)
            {
                _logger?.Invoke($"[ComputerPlayer]: Согласен на ничью.");
                IsTieAccepted = true;
                InvokeOnTieRequest();
            }
            else
            {
                _logger?.Invoke($"[ComputerPlayer]: Отказался от ничьей.");
                IsTieAccepted = false;
            }
            await Task.CompletedTask;
        }

        public override async Task OnGameStarted()
        {
            _logger?.Invoke($"[ComputerPlayer]: Игра началась для {Identifier}.");
            await Task.CompletedTask;
        }

        public override async Task OnGameEnded(string status, int player1Move, int player2Move)
        {
            _logger?.Invoke($"[ComputerPlayer]: Игра окончена. Статус: {status}");
            await Task.CompletedTask;
        }
    }
}
