
using TopNetwork.Core;
using TopNetwork.RequestResponse;

namespace WPFTasks.Core.Models.TicTacToe.Services.TicTacToeLogic.GameCores
{
    internal class CCGameCore : TicTacToeGame
    {
        public TopClient Initiator { get; private set; }
        public CCGameCore(TopClient initiator, LogString? logger = null) : base(new ComputerPlayer('X', logger), new ComputerPlayer('O', logger), logger) 
        { 
            Initiator = initiator;
            OnGameEnded += CCGameCore_OnGameEnded;
            OnGameStarted += CCGameCore_OnGameStarted;
            
        }

        private void CCGameCore_OnGameStarted()
        {
            throw new NotImplementedException();
        }

        private void CCGameCore_OnGameEnded(string obj)
        {
            throw new NotImplementedException();
        }
    }
}
