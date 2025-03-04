
using TopNetwork.RequestResponse;

namespace WPFTasks.Core.Models.RockPaperScissors.Services.RockPaperScissorsLogic
{
    public enum GameTypes
    {
        Human_Human,
        Human_Computer,
        Computer_Computer
    }


    public class RPSGame
    {
        public BasePlayer Player1 { get; }
        public BasePlayer Player2 { get; }
        public LogString? Logger { get; set; }

        public event Action? OnGameEnded;

        public RPSGame(BasePlayer player1, BasePlayer player2, LogString? logger = null)
        {
            Player1 = player1;
            Player2 = player2;
            Logger = logger;
        }

        // Определение победителя: 0 – ничья, 1 – выигрывает Player1, 2 – выигрывает Player2.
        private int DetermineWinner(int move1, int move2)
        {
            if (move1 == move2) return 0;
            if ((move1 == 0 && move2 == 2) ||
                (move1 == 1 && move2 == 0) ||
                (move1 == 2 && move2 == 1))
                return 1;
            return 2;
        }

        public async Task Start(CancellationToken token)
        {
            await Player1.OnGameStarted();
            await Player2.OnGameStarted();

            // Запрос ходов у обоих игроков параллельно.
            var t1 = Task.Run(async () =>
            {
                var tcs = new TaskCompletionSource<int>();
                void Handler(int move)
                {
                    tcs.TrySetResult(move);
                    Player1.OnGetPlayerMove -= Handler;
                }
                Player1.OnGetPlayerMove += Handler;
                await Player1.OnPlayerTurn();
                return await tcs.Task;
            }, token);

            var t2 = Task.Run(async () =>
            {
                var tcs = new TaskCompletionSource<int>();
                void Handler(int move)
                {
                    tcs.TrySetResult(move);
                    Player2.OnGetPlayerMove -= Handler;
                }
                Player2.OnGetPlayerMove += Handler;
                await Player2.OnPlayerTurn();
                return await tcs.Task;
            }, token);

            int move1 = await t1;
            int move2 = await t2;

            int winner = DetermineWinner(move1, move2);
            string result;
            if (winner == 0)
                result = "Ничья";
            else if (winner == 1)
                result = $"{Player1.Name} выигрывает";
            else
                result = $"{Player2.Name} выигрывает";

            await Player1.OnResultTurn(result);
            await Player2.OnResultTurn(result);

            Logger?.Invoke($"[RPSGame]: Результат игры - {result}. Ходы: {move1} и {move2}");

            OnGameEnded?.Invoke();
        }
    }
}
