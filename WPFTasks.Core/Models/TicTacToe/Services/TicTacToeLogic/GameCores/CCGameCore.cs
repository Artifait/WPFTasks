
using TopNetwork.Core;

namespace WPFTasks.Core.Models.TicTacToe.Services.TicTacToeLogic.GameCores
{
    internal class CCGameCore : TicTacToeGame
    {
        public TopClient Initiator { get; private set; }
        public CCGameCore(TopClient initiator) : base(new ComputerPlayer('X'), new ComputerPlayer('O')) { Initiator = initiator; }

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
