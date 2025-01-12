
using TopNetwork.Core;
using TopNetwork.RequestResponse;
using TopNetwork.Services.MessageBuilder;
using WPFTasks.Core.Models.TicTacToe.MessageBuilder;

namespace WPFTasks.Core.Models.TicTacToe.Services.TicTacToeLogic
{
    internal class HumanPlayer : BasePlayer
    {
        // ТО что мы отправляем игроку 
        private static MessageBuilderService _msgService = new MessageBuilderService()
            .Register(() => new ErroreMessageBuilder())         // Ошибка
            .Register(() => new PlayerTurnMsgBuilder())         // Для уведомления игрока что сейчас его ход 
            .Register(() => new TieGameRequestMsgBuilder())     // Для того чтобы предложить клиенту ничью, когда другой попросил
            .Register(() => new GameEndedMsgBuilder())          // Когда игра кончилась
            .Register(() => new UpdateGameBoardMsgBuilder());   // ДЛя уведомления об обновлении доски         

        public char Symbol { get; private set; }
        public TopClient Client { get; private set; }
        public LogString? Logger { get; set; }

        public HumanPlayer(TopClient client, char symbol, LogString? logger = null)
        {
            Client = client;
            Symbol = symbol;
            Logger = logger;

            Client.OnMessageReceived += Client_OnMessageReceived;
            Client.OnConnectionLost += InvokeOnConcession;
        }

        private async Task Client_OnMessageReceived(Message msg)
        {
            if (msg.MessageType == UserMoveData.MsgType)
            {
                try
                {
                    var data = UserMoveMsgBuilder.Parse(msg);
                    InvokeOnGetPlayerMove((data.CellNumber % 3, data.CellNumber / 3));
                }
                catch (Exception ex)
                {
                    Logger?.Invoke($"[Server]: Ошибка обработки хода от [{Client.RemoteEndPoint}].\n{ex.Message}");
                    await Client.SendMessageAsync(_msgService.BuildMessage<ErroreMessageBuilder, ErroreData>(builder => builder
                        .SetPayload($"Невозможно обработать ход.\n{ex.Message}")
                    ));
                }
            }
            if (msg.MessageType == EndGameRequestData.MsgType)
            {
                InvokeOnConcession();
            }
            if (msg.MessageType == TieGameRequestData.MsgType)
            {
                InvokeOnTieRequest();
            }
        }

        public override async Task OnGameEnded(string status)
        {
            try
            {
                var response = _msgService.BuildMessage<GameEndedMsgBuilder, GameEndedData>(builder => builder
                    .SetGameStatus(status)
                );
                await Client.SendMessageAsync(response);
            }
            catch (Exception ex)
            {
                Logger?.Invoke($"[HumanPlayer]: Ошибка при отправке уведомление о завершении игры: {ex.Message}");
            }
        }

        public override async Task OnGameStarted()
        {
            try
            {
                var response = _msgService.BuildMessage<GameStartedMsgBuilder, GameStartedData>();
                await Client.SendMessageAsync(response);
            }
            catch (Exception ex)
            {
                Logger?.Invoke($"[HumanPlayer]: Ошибка при отправке уведомление о старте игры: {ex.Message}");
            }
        }

        public override async Task OnPlayerTurn(Board board)
        {
            try
            {
                var notification = _msgService.BuildMessage<PlayerTurnMsgBuilder, PlayerTurnData>(builder => builder
                    .SetBoard(board.GetState())
                );

                await Client.SendMessageAsync(notification);
            }
            catch (Exception ex)
            {
                Logger?.Invoke($"[HumanPlayer]: Ошибка при отправке уведомление, что настпуил ход игрока: {ex.Message}");
            }
        }


    }
}
