
using TopNetwork.Core;
using TopNetwork.RequestResponse;

namespace WPFTasks.Core.Models.TicTacToe.Services.TicTacToeLogic.GameCores
{
    internal class HHGameCore : TicTacToeGame// HH -> Human And Human
    {
        public new OnlinePlayer PlayerX { get; private set; }
        public new OnlinePlayer PlayerO { get; private set; }

        public HHGameCore(TopClient playerX, TopClient playerO, LogString? logger = null) : base(new OnlinePlayer(playerX, 'X', logger), new OnlinePlayer(playerO, 'O', logger), logger)
        {
            PlayerX = (OnlinePlayer)base.PlayerX;
            PlayerO = (OnlinePlayer)base.PlayerO;
        }
    }
}
