
using TopNetwork.Core;

namespace WPFTasks.Core.Models.TicTacToe.Services.TicTacToeLogic.GameCores
{
    internal class HCGameCore : TicTacToeGame
    {
        private readonly Random _random = new();
        public new HumanPlayer PlayerX { get; private set; }
        public new ComputerPlayer PlayerO { get; private set; }

        public HCGameCore(TopClient playerX) : base(new HumanPlayer(playerX, 'X'), new ComputerPlayer('O'))
        {
            PlayerX = (HumanPlayer)base.PlayerX;
            PlayerO = (ComputerPlayer)base.PlayerO;
        }

        public override async Task Start(CancellationToken token)
        {

            await Task.CompletedTask;
        }

        protected override async Task GameLoop(CancellationToken token)
        {

            await Task.CompletedTask;
        }
    }
}
