
using System.Windows.Documents;
using TopNetwork.Core;
using TopNetwork.RequestResponse;
using TopNetwork.Services.MessageBuilder;
using WPFTasks.Core.Models.PcStore;
using WPFTasks.Core.Models.TicTacToe.MessageBuilder;

namespace WPFTasks.Core.Models.TicTacToe.Services.TicTacToeLogic.GameCores
{
    internal class CCGameCore : TicTacToeGame
    {
        private static MessageBuilderService _msgService = new MessageBuilderService()
            .Register(() => new ErroreMessageBuilder())         // Ошибка
            .Register(() => new GameEndedMsgBuilder())          // Когда игра кончилась
            .Register(() => new UpdateGameBoardMsgBuilder())
            .Register(() => new GameStartedMsgBuilder());   // ДЛя уведомления об обновлении доски    

        public TopClient Initiator { get; private set; }
        public CCGameCore(TopClient initiator, LogString? logger = null) : base(new ComputerPlayer('X', logger), new ComputerPlayer('O', logger), logger) 
        { 
            Initiator = initiator;
            OnGameEnded += CCGameCore_OnGameEnded;
            OnGameStarted += CCGameCore_OnGameStarted;
            OnBoardUpdate += CCGameCore_OnBoardUpdate;
        }

        private async void CCGameCore_OnBoardUpdate(char[,] board, char turnSymbol)
        {
            try
            {
                var notification = _msgService.BuildMessage<UpdateGameBoardMsgBuilder, UpdateGameBoardData>(builder => builder
                    .SetBoard(board)
                    .SetTurnSymbol(turnSymbol)
                );

                await Initiator.SendMessageAsync(notification);
            }
            catch (Exception ex)
            {
                Logger?.Invoke($"[CCGameCore]: Ошибка при отправке уведомление об обновлении доски: {ex.Message}");
            }
        }

        private async void CCGameCore_OnGameStarted()
        {
            try
            {
                var response = _msgService.BuildMessage<GameStartedMsgBuilder, GameStartedData>();
                await Initiator.SendMessageAsync(response);
            }
            catch (Exception ex)
            {
                Logger?.Invoke($"[CCGameCore]: Ошибка при отправке уведомление о старте игры: {ex.Message}");
            }
        }

        private async void CCGameCore_OnGameEnded(string status)
        {
            try
            {
                var response = _msgService.BuildMessage<GameEndedMsgBuilder, GameEndedData>(builder => builder
                    .SetGameStatus(status)
                    .SetBoard(Board.GetState())
                );
                await Initiator.SendMessageAsync(response);
            }
            catch (Exception ex)
            {
                Logger?.Invoke($"[CCGameCore]: Ошибка при отправке уведомление о завершении игры: {ex.Message}");
            }
        }


    }
}
