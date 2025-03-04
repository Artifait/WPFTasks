using TopNetwork.Core;
using TopNetwork.RequestResponse;

namespace WPFTasks.Core.Models.RockPaperScissors.Services.RockPaperScissorsLogic.GameCores
{
    public class CCGameCore : RPSGame
    {
        public ComputerPlayer Computer1 { get; }
        public ComputerPlayer Computer2 { get; }
        public TopClient Initiator { get; }

        public CCGameCore(TopClient initiator, LogString? logger = null)
            : base(new ComputerPlayer("Computer1", logger), new ComputerPlayer("Computer2", logger), logger)
        {
            Computer1 = (ComputerPlayer)base.Player1;
            Computer2 = (ComputerPlayer)base.Player2;
            Initiator = initiator;
        }
    }
}
