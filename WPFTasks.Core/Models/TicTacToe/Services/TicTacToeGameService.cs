
using System.Collections.Concurrent;
using TopNetwork.Core;
using TopNetwork.Services.MessageBuilder;
using WPFTasks.Core.Models.TicTacToe.MessageBuilder;

namespace WPFTasks.Core.Models.TicTacToe.Services
{
    public class TicTacToeGameService
    {
        private readonly MessageBuilderService _msgService;
        private readonly ConcurrentDictionary<TopClient, TicTacToeSession> _sessions = [];
        private readonly object _locker = new();
        private TopClient? _finderGameUserAndUser;

        public async Task<Message> FindGameToClient(TopClient client, FindGameRequestData requestData)
        {
            switch (requestData.GameType)
            {
                case GameTypes.Human_Human:
                    lock (_locker)
                    {
                        if(_finderGameUserAndUser == null)
                        {
                            _finderGameUserAndUser = client;
                            break;
                        }
                        TopClient playerO = client;
                        TopClient playerX = _finderGameUserAndUser;// Ходит первым, тк ждал второго
                        _finderGameUserAndUser = null;

                        StartGameUserAndUser(playerX: playerX, playerO: playerO);
                    }
                    break;
            }
        }


        private async Task StartGameUserAndUser(TopClient playerX, TopClient playerO)
        {

        }
    }

    public class TicTacToeSession
    {
        private readonly CancellationTokenSource _tkc;

        public event Action? SessionClosed;
        public GameTypes GameType { get; private set; }
        public TicTacToeGame GameCore { get; private set; }


        public TicTacToeSession(TopClient playerX, TopClient playerO)
        {
            GameCore = new HHGameCore(playerX, playerO);
            GameType = GameTypes.Human_Human;
        }

        public TicTacToeSession(TopClient playerX)
        {
            
            GameType = GameTypes.Human_Computer;
        }

        public async Task StartGame(CancellationToken token)
        {
            
        }
    }

    public enum GameEndState
    {
        Winner,
        Draw,
        ForcedEnd,// При отключкении игрока
        Concession// При EndGameRequest
    }

    public interface IPlayer
    {
        char Symbol { get; }
        event Action<(int x, int y)?> OnGetPlayerMove;  // Когда получили ход от игрока
        event Action<Board> OnPlayerTurn;               // Когда настал ход игрока
    }

    public enum GameTypes
    {
        Human_Human,
        Human_Computer,
        Computer_Computer
    }

    public abstract class TicTacToeGame
    {
        public IPlayer PlayerX { get; private set; }
        public IPlayer PlayerO { get; private set; }
        public Board Board { get; private set; }

        public bool IsEnded { get; protected set; } = false;
        public bool IsStarted { get; protected set; }
        public bool IsRunning => !IsEnded && IsStarted;

        public event Action<char[,]>? OnBoardUpdated;
        public event Action<string>? OnGameEnded;
        public event Action? OnGameStarted;

        public TicTacToeGame(IPlayer playerX, IPlayer playerO)
        {
            PlayerO = playerO;
            PlayerX = playerX;
            Board = new Board();
        }

        public virtual async Task Start(CancellationToken token) { throw new NotImplementedException(); }
        protected virtual async Task GameLoop(CancellationToken token) { throw new NotImplementedException(); }
    }

    internal class HumanPlayer : IPlayer
    {
        public event Action<(int x, int y)?> OnGetPlayerMove;
        public event Action<Board> OnPlayerTurn;

        public char Symbol { get; private set; }
        public TopClient Client { get; private set; }

        public HumanPlayer(TopClient client, char symbol) 
        {
            Client = client;
            Symbol = symbol;
        }

    }

    internal class HHGameCore : TicTacToeGame// HH -> Human And Human
    {
        public new HumanPlayer PlayerX { get; private set; }
        public new HumanPlayer PlayerO { get; private set; }

        public HHGameCore(TopClient playerX, TopClient playerO) : base(new HumanPlayer(playerX, 'X'), new HumanPlayer(playerO, 'O'))
        {
            PlayerX = (HumanPlayer)base.PlayerX;
            PlayerO = (HumanPlayer)base.PlayerO;
        }
    }

    internal class ComputerPlayer : IPlayer
    {
        private static readonly Random random = new();

        public char Symbol { get; private set; }

        public event Action<(int x, int y)?> OnGetPlayerMove;
        public event Action<Board> OnPlayerTurn;

        public ComputerPlayer(char symbol)
        {
            Symbol = symbol;
            OnPlayerTurn += (board) => OnGetPlayerMove?.Invoke(OnPlayerTurnHandler(board).Result);
        }

        private static async Task<(int x, int y)?> OnPlayerTurnHandler(Board board)
        {
            if (board.IsFull()) return (-1, -1);
            int x, y;

            do
            {
                x = random.Next(0, 3);
                y = random.Next(0, 3);
            } while (!board.IsCellEmpty(x, y));

            return await Task.FromResult((x, y));
        }
    }
}
