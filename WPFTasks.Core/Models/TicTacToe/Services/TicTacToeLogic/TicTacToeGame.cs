
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
        public BasePlayer PlayerX { get; private set; }
        public BasePlayer PlayerO { get; private set; }
        public Board Board { get; private set; }

        public bool IsEnded { get; protected set; } = false;
        public bool IsStarted { get; protected set; } = false;
        public LogString Logger { get; set; }
        public bool IsRunning => !IsEnded && IsStarted;

        public event Action<string>? OnGameEnded;
        public event Action? OnGameStarted;

        public TicTacToeGame(BasePlayer playerX, BasePlayer playerO)
        {
            PlayerO = playerO;
            PlayerX = playerX;
            Board = new Board();
            
            OnGameEnded += async status => { await PlayerO.OnGameEnded(status); await PlayerX.OnGameEnded(status); };
            OnGameStarted += () => { PlayerO.OnGameStarted(); PlayerX.OnGameStarted(); };
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

            // Запускаем основной игровой цикл
            await GameLoop(token);
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
                    currentPlayer.OnPlayerTurn(Board);

                    // Ждем хода
                    var move = await moveCompletion.Task;

                    if (move.HasValue)
                    {
                        var (x, y) = move.Value;

                        if (Board.MakeMove(x, y, currentPlayer.Symbol))
                        {
                            await currentPlayer.OnResultTurn(Board);
                            Logger?.Invoke($"Player {currentPlayer.Symbol} made a move at ({x}, {y}).");

                            // Проверяем на победу или ничью
                            var winner = Board.CheckWinner();
                            if (winner != Board.Empty)
                            {
                                IsEnded = true;
                                OnGameEnded?.Invoke($"Player {winner} wins!");
                                Logger?.Invoke($"Game ended. Player {winner} wins!");
                                break;
                            }

                            if (Board.IsFull())
                            {
                                IsEnded = true;
                                OnGameEnded?.Invoke("It's a tie!");
                                Logger?.Invoke("Game ended in a tie!");
                                break;
                            }

                            // Передача хода следующему игроку
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
                    break;
                }
            }
        }
    }
}
