
using TopNetwork.RequestResponse;

namespace WPFTasks.Core.Models.TicTacToe.Services.TicTacToeLogic
{
    public enum GameTypes
    {
        Human_Human,
        Human_Computer,
        Computer_Computer
    }

    public class TicTacToeGame
    {
        private CancellationTokenSource? _cts = null;

        public BasePlayer PlayerX { get; private set; }
        public BasePlayer PlayerO { get; private set; }
        public Board Board { get; private set; }

        public bool IsEnded { get; protected set; } = false;
        public bool IsStarted { get; protected set; } = false;
        public LogString? Logger { get; set; }
        public bool IsRunning => !IsEnded && IsStarted;

        public event Action<string>? OnGameEnded;
        public event Action? OnGameStarted;
        public event Action<char[,], char> OnBoardUpdate;

        public TicTacToeGame(BasePlayer playerX, BasePlayer playerO, LogString? logger = null)
        {
            PlayerO = playerO;
            PlayerX = playerX;
            Board = new Board();
            Logger = logger;
            
            OnGameEnded += async status => {
                try { PlayerO.DisposeEvent(); } catch { }
                try { PlayerX.DisposeEvent(); } catch { }
                try { await PlayerO.OnGameEnded(status, Board.GetState()); } catch { }
                try { await PlayerX.OnGameEnded(status, Board.GetState()); } catch { } 
            };

            OnGameStarted += async () => {
                try { await PlayerO.OnGameStarted(); } catch { }
                try { await PlayerX.OnGameStarted(); } catch { }
            };

            PlayerO.OnTieRequest += async () => await OnTieRequest(PlayerX);
            PlayerX.OnTieRequest += async () => await OnTieRequest(PlayerO);

            PlayerO.OnConcession += async () => await OnConcession(PlayerO);
            PlayerX.OnConcession += async () => await OnConcession(PlayerX);
        }

        private SemaphoreSlim _concessionSemaphore = new(1, 1);
        private async Task OnConcession(BasePlayer sender)
        {
            await _concessionSemaphore.WaitAsync();
            try
            {
                TryEndGame($"Игрок за {sender.Symbol} сдался!");
            }
            finally { _concessionSemaphore.Release(); }

        }

        private SemaphoreSlim _tieSemaphore = new(1, 1);
        private async Task OnTieRequest(BasePlayer secondPlayer)
        {
            await _tieSemaphore.WaitAsync();
            try
            {
                if (PlayerO.IsTieAccepted && PlayerX.IsTieAccepted)
                {
                    TryEndGame("НИЧЬЯ!!!");
                    return;
                }
                _ = secondPlayer.OnTieOffered();
            }
            finally { _tieSemaphore.Release(); }

        }

        public virtual async Task Start(CancellationToken token)
        {
            if (IsStarted)
                throw new InvalidOperationException("The game is already running!");
            if(IsEnded)
                throw new InvalidOperationException("The game is already over!");


            IsStarted = true;
            OnGameStarted?.Invoke(); // Уведомляем, что игра началась
            Logger?.Invoke("Game started!");
            _cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            // Запускаем основной игровой цикл
            await GameLoop(_cts.Token);
        }
        protected virtual async Task GameLoop(CancellationToken token)
        {
            // Указываем, кто первый ходит
            BasePlayer currentPlayer = PlayerX;
            Logger?.Invoke($"[TicTacToeGame]: Player {currentPlayer.Symbol} starts the game!");

            while (!IsEnded && !token.IsCancellationRequested)
            {
                try
                {
                    // Подписываемся на событие получения хода от игрока
                    TaskCompletionSource<(int x, int y)?> moveCompletion = new();
                    void OnMoveReceived((int x, int y)? move)
                    {
                        moveCompletion.TrySetResult(move);
                        currentPlayer.OnGetPlayerMove -= OnMoveReceived; // Отписываемся после получения хода
                    }

                    // Уведомляем текущего игрока, что он должен сделать ход
                    currentPlayer.OnGetPlayerMove += OnMoveReceived;
                    await currentPlayer.OnPlayerTurn(Board);

                    // Ждем хода
                    var move = await moveCompletion.Task;

                    if (move.HasValue)
                    {
                        var (x, y) = move.Value;

                        if (Board.MakeMove(x, y, currentPlayer.Symbol))
                        {
                            OnBoardUpdate?.Invoke(Board.GetState(), currentPlayer == PlayerX ? 'O' : 'X');
                            Logger?.Invoke($"Player {currentPlayer.Symbol} made a move at ({x}, {y}).");

                            var winner = Board.CheckWinner();
                            if (winner != Board.Empty)
                            {
                                TryEndGame($"Player {winner} wins!");
                                Logger?.Invoke($"Game ended. Player {winner} wins!");
                                break;
                            }

                            if (Board.IsFull())
                            {
                                TryEndGame("Ничья!");
                                Logger?.Invoke("Game ended in a tie!");
                                break;
                            }

                            await currentPlayer.OnResultTurn(Board);
                            currentPlayer = currentPlayer == PlayerX ? PlayerO : PlayerX;
                        }
                        else
                        {
                            Logger?.Invoke($"Invalid move by player {currentPlayer.Symbol} at ({x}, {y}).");
                        }
                    }
                    else
                    {
                        Logger?.Invoke($"Player {currentPlayer.Symbol} failed to make a move.");
                    }

                    await Task.Delay(500, token);
                }
                catch (TaskCanceledException)
                {
                    Logger?.Invoke("Game loop cancelled.");
                    OnGameEnded?.Invoke("Вашу игру закрыли с вышееее");
                    break;
                }
            }

            await Task.CompletedTask;
        }

        private void TryEndGame(string status)
        {
            if (IsEnded)
                return;

            IsEnded = true;
            OnGameEnded?.Invoke(status);
        }
    }
}
