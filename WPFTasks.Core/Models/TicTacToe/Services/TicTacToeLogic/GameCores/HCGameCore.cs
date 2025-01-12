
using TopNetwork.Core;
using TopNetwork.RequestResponse;
using WPFTasks.Core.Models.Core;

namespace WPFTasks.Core.Models.TicTacToe.Services.TicTacToeLogic.GameCores
{
    internal class HCGameCore : TicTacToeGame
    {
        private readonly Random _random = new();
        public new OnlinePlayer PlayerX { get; private set; }
        public new ComputerPlayer PlayerO { get; private set; }

        public HCGameCore(TopClient playerX, LogString? logger = null) : base(new OnlinePlayer(playerX, 'X', logger), new ComputerPlayer('O', logger), logger)
        {
            PlayerX = (OnlinePlayer)base.PlayerX;
            PlayerO = (ComputerPlayer)base.PlayerO;
        }
    }
}
