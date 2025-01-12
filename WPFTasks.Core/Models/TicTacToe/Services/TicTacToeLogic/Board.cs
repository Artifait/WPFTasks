
namespace WPFTasks.Core.Models.TicTacToe.Services.TicTacToeLogic
{
    public class Board
    {
        private readonly char[,] _board = new char[3, 3];
        public const char Empty = ' ';

        public Board()
        {
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                    _board[i, j] = Empty;
        }

        public bool IsCellEmpty(int x, int y) => _board[x, y] == Empty;

        public bool MakeMove(int x, int y, char symbol)
        {
            if (!IsCellEmpty(x, y)) return false;
            _board[x, y] = symbol;
            return true;
        }

        public char[,] GetState() => _board;

        public bool IsFull() => _board.Cast<char>().All(cell => cell != Empty);

        public char CheckWinner()
        {
            for (int i = 0; i < 3; i++)
            {
                if (_board[i, 0] != Empty && _board[i, 0] == _board[i, 1] && _board[i, 1] == _board[i, 2])
                    return _board[i, 0];
                if (_board[0, i] != Empty && _board[0, i] == _board[1, i] && _board[1, i] == _board[2, i])
                    return _board[0, i];
            }

            if (_board[0, 0] != Empty && _board[0, 0] == _board[1, 1] && _board[1, 1] == _board[2, 2])
                return _board[0, 0];
            if (_board[0, 2] != Empty && _board[0, 2] == _board[1, 1] && _board[1, 1] == _board[2, 0])
                return _board[0, 2];

            return Empty;
        }
    }
}
