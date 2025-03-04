using TopNetwork.Core;
using TopNetwork.RequestResponse;

namespace WPFTasks.Core.Models.RockPaperScissors.Services.RockPaperScissorsLogic.GameCores
{
    public class HCGameCore : RPSGame
    {
        public OnlinePlayer Player { get; }
        public ComputerPlayer Computer { get; }

        public HCGameCore(TopClient client, LogString? logger = null)
            : base(new OnlinePlayer(client, "Player", logger), new ComputerPlayer("Computer", logger), logger)
        {
            Player = (OnlinePlayer)base.Player1;
            Computer = (ComputerPlayer)base.Player2;
        }
    }
}
