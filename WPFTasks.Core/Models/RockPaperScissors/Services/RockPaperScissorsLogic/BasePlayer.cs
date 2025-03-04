using TopNetwork.RequestResponse;

namespace WPFTasks.Core.Models.RockPaperScissors.Services.RockPaperScissorsLogic
{
    public abstract class BasePlayer
    {
        // В RPS ход представляется числом: 0 - Rock, 1 - Paper, 2 - Scissors.
        public string Name { get; protected set; } = "Player";
        public bool IsTieAccepted { get; protected set; } = false;

        public LogString? Logger { get; protected set; }

        public event Action<int>? OnGetPlayerMove;  // Получение хода игрока
        public event Action? OnTieRequest;          // Запрос ничьей
        public event Action? OnConcession;          // Сдача

        protected void InvokeOnGetPlayerMove(int move) => OnGetPlayerMove?.Invoke(move);
        protected void InvokeOnConcession() => OnConcession?.Invoke();
        protected void InvokeOnTieRequest() => OnTieRequest?.Invoke();

        public virtual async Task OnPlayerTurn() { }
        public virtual async Task OnResultTurn(string result) { }
        public virtual async Task OnTieOffered() { }
        public virtual async Task OnGameEnded(string result, int player1Move, int player2Move) { }
        public virtual async Task OnGameStarted() { }

        public void DisposeEvent()
        {
            OnGetPlayerMove = null;
            OnTieRequest = null;
            OnConcession = null;
        }
    }
}
