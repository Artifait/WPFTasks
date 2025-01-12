
using TopNetwork.RequestResponse;

namespace WPFTasks.Core.Models.TicTacToe.Services.TicTacToeLogic
{
    public abstract class BasePlayer
    {
        public char Symbol { get; protected set; }
        public bool IsTieAccepted { get; protected set; } = false;
        public LogString? Logger { get; protected set; }

        public event Action<(int x, int y)?>? OnGetPlayerMove;  // Для получения хода игрока(Core должен подписаться сюда)  
        public event Action? OnTieRequest;                      // Для уведомления Core что нужно отправить второму игроку уведомления что клиент хочет ничью
        public event Action? OnConcession;                      // ДЛя уведомления Core что игрок признаёт поражению до окончания игры

        protected void InvokeOnGetPlayerMove((int x, int y) data) => OnGetPlayerMove?.Invoke(data);
        protected void InvokeOnConcession() => OnConcession?.Invoke();
        protected void InvokeOnTieRequest() => OnTieRequest?.Invoke();

        public virtual async Task OnPlayerTurn(Board board) { }     // Когда настал ход игрока
        public virtual async Task OnResultTurn(Board board) { }     // Для отображение хода игрока
        public virtual async Task OnTieOffered() { }                // Когда второй игрок предложил ничью
        public virtual async Task OnGameEnded(string status, char[,] board) { }   
        public virtual async Task OnGameStarted() { }

        public void DisposeEvent()
        {
            OnGetPlayerMove = null;
            OnTieRequest = null;
            OnConcession = null;
        }
    }
}
