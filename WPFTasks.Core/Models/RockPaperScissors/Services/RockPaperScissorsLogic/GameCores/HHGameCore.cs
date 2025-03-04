using TopNetwork.Core;
using TopNetwork.RequestResponse;

namespace WPFTasks.Core.Models.RockPaperScissors.Services.RockPaperScissorsLogic.GameCores
{
    public class HHGameCore : RPSGame
    {
        public OnlinePlayer Player1 { get; }
        public OnlinePlayer Player2 { get; }

        public HHGameCore(TopClient client1, TopClient client2, LogString? logger = null)
            : base(new OnlinePlayer(client1, "Player1", logger), new OnlinePlayer(client2, "Player2", logger), logger)
        {
            Player1 = (OnlinePlayer)base.Player1;
            Player2 = (OnlinePlayer)base.Player2;
        }
    }
}
