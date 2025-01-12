
namespace WPFTasks.Core.Models.TicTacToe.Services.TicTacToeLogic
{
    internal class ComputerPlayer : BasePlayer
    {
        private static readonly Random random = new();

        public char Symbol { get; private set; }

        public event Action<(int x, int y)?>? OnGetPlayerMove;
        public event Action<Board>? OnPlayerTurn;
        public event Action? OnTieRequest;
        public event Action? OnConcession;
        public event Action<string> OnGameEnded;

        public ComputerPlayer(char symbol)
        {
            Symbol = symbol;
            OnPlayerTurn += (board) => OnGetPlayerMove?.Invoke(OnPlayerTurnHandler(board).Result);
        }

        private static async Task<(int x, int y)?> OnPlayerTurnHandler(Board board)
        {
            int x, y;

            do
            {
                await Task.Delay(500);
                x = random.Next(0, 3);
                y = random.Next(0, 3);
            } while (!board.IsCellEmpty(x, y));

            return await Task.FromResult((x, y));
        }

        public void InvokeOnPlayerTurn(Board board)
        {
            OnPlayerTurn?.Invoke(board);
        }

        public void InvokeOnGameEnded(string status)
        {
            OnGameEnded?.Invoke(status);
        }
    }
}
