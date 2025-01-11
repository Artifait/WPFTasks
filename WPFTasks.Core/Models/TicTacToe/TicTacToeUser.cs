
using TopNetwork.Services;

namespace WPFTasks.Core.Models.TicTacToe
{
    public class TicTacToeUser : User
    {
        public TicTacToeUser(string login, string passwordHash) : base(login, passwordHash)
        {
        }
    }
}
