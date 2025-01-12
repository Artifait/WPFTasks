
using TopNetwork.RequestResponse;

namespace WPFTasks.Core.Models.TicTacToe.Services.TicTacToeLogic
{
    internal class ComputerPlayer : BasePlayer
    {
        private static readonly Random _random = new();
        private bool _hasRespondedToTieRequest = false; 
        private readonly LogString? _logger;

        public ComputerPlayer(char symbol, LogString? logger = null)
        {
            Symbol = symbol;
            _logger = logger;
        }

        public override async Task OnPlayerTurn(Board board)
        {
            await Task.Delay(_random.Next(500, 1500));

            var availableCells = board.GetAvailableCells();
            if (availableCells.Any())
            {
                var chosenCell = availableCells[_random.Next(availableCells.Count)];
                InvokeOnGetPlayerMove(chosenCell);
            }
        }

        public override async Task OnResultTurn(Board board)
        {
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

            bool acceptTie = _random.Next(0, 100) < 30;

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
            _logger?.Invoke($"[ComputerPlayer]: Игра началась.");
            await Task.CompletedTask;
        }

        public override async Task OnGameEnded(string status, char[,] board)
        {
            _logger?.Invoke($"[ComputerPlayer]: Игра окончена. Статус: {status}");
            await Task.CompletedTask;
        }
    }
}
