
using TopNetwork.Core;

namespace WPFTasks.Core.Models.TicTacToe.Services.TicTacToeLogic.GameCores
{
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
}
