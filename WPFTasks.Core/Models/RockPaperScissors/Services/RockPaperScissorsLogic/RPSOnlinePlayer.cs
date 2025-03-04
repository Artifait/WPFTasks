using TopNetwork.Core;
using TopNetwork.RequestResponse;
using TopNetwork.Services.MessageBuilder;
using WPFTasks.Core.Models.RockPaperScissors.MessageBuilder;
using WPFTasks.Core.Models.RockPaperScissors.Services.RockPaperScissorsLogic;

namespace WPFTasks.Core.Models.RockPaperScissors.Services.RockPaperScissorsLogic
{
    public class OnlinePlayer : BasePlayer
    {
        // Сервис для формирования сообщений для игры "Камень, ножницы, бумага"
        private static MessageBuilderService _msgService = new MessageBuilderService()
            .Register(() => new ErroreMessageBuilder())         // Ошибка
            .Register(() => new PlayerTurnMsgBuilder())         // Уведомление о том, что сейчас ход игрока
            .Register(() => new TieGameRequestMsgBuilder())     // Для предложения ничьи
            .Register(() => new GameEndedMsgBuilder())          // Оповещение о завершении игры
            .Register(() => new GameStartedMsgBuilder())        // Оповещение о начале игры
            .Register(() => new RoundResultMsgBuilder());       // Оповещение о результате раунда

        public TopClient Client { get; private set; }
        // В RPS вместо символа можно использовать идентификатор игрока (например, имя)
        public string Identifier { get; private set; }

        public OnlinePlayer(TopClient client, string identifier, LogString? logger = null)
        {
            Client = client;
            Identifier = identifier;
            Logger = logger;

            Client.OnMessageReceived += Client_OnMessageReceived;
            Client.OnConnectionLost += InvokeOnConcession;
        }

        private async Task Client_OnMessageReceived(Message msg)
        {
            // Если получен ход игрока (UserMove) – передаём его в событие.
            if (msg.MessageType == UserMoveData.MsgType)
            {
                try
                {
                    var data = UserMoveMsgBuilder.Parse(msg);
                    // Передаём просто числовой ход (0, 1 или 2)
                    InvokeOnGetPlayerMove(data.Move);
                }
                catch (Exception ex)
                {
                    Logger?.Invoke($"[Server]: Ошибка обработки хода от [{Client.RemoteEndPoint}].\n{ex.Message}");
                    await Client.SendMessageAsync(_msgService.BuildMessage<ErroreMessageBuilder, ErroreData>(builder => builder
                        .SetPayload($"Невозможно обработать ход.\n{ex.Message}")
                    ));
                }
            }
            // При получении сообщения о завершении игры – вызываем событие сдачи.
            if (msg.MessageType == EndGameRequestData.MsgType)
            {
                InvokeOnConcession();
            }
            // Если получено сообщение о ничье – помечаем, что игрок принял ничью.
            if (msg.MessageType == TieGameRequestData.MsgType)
            {
                IsTieAccepted = true;
                InvokeOnTieRequest();
            }
        }

        // Оповещаем клиента о завершении игры:
        // Передаём итоговый результат и ходы обоих игроков (преобразованные в символы: 'R', 'P', 'S')
        public override async Task OnGameEnded(string result, int player1Move, int player2Move)
        {
            try
            {
                Client.OnMessageReceived -= Client_OnMessageReceived;

                char moveChar1 = ConvertMoveToChar(player1Move);
                char moveChar2 = ConvertMoveToChar(player2Move);

                var response = _msgService.BuildMessage<GameEndedMsgBuilder, GameEndedData>(builder => builder
                    .SetGameResult(result)
                    .SetPlayerMoves(moveChar1, moveChar2)
                );
                await Client.SendMessageAsync(response);
            }
            catch (Exception ex)
            {
                Logger?.Invoke($"[OnlinePlayer]: Ошибка при отправке уведомления о завершении игры: {ex.Message}");
            }
        }

        // При старте игры отправляем инструкции
        public override async Task OnGameStarted()
        {
            try
            {
                var response = _msgService.BuildMessage<GameStartedMsgBuilder, GameStartedData>(builder => builder
                    .SetInstructions("Выберите ход: R - Камень, P - Бумага, S - Ножницы")
                );
                await Client.SendMessageAsync(response);
            }
            catch (Exception ex)
            {
                Logger?.Invoke($"[OnlinePlayer]: Ошибка при отправке уведомления о старте игры: {ex.Message}");
            }
        }

        // Уведомление о том, что игрок должен сделать ход
        public override async Task OnPlayerTurn()
        {
            try
            {
                var notification = _msgService.BuildMessage<PlayerTurnMsgBuilder, PlayerTurnData>(builder => builder
                    .SetPrompt("Ваш ход. Введите R, P или S.")
                );

                await Client.SendMessageAsync(notification);
            }
            catch (Exception ex)
            {
                Logger?.Invoke($"[OnlinePlayer]: Ошибка при отправке уведомления о ходе: {ex.Message}");
            }
        }

        // Уведомляем игрока о результате раунда (например, "Ничья" или "Выиграл игрок X")
        public override async Task OnResultTurn(string result)
        {
            try
            {
                var notification = _msgService.BuildMessage<RoundResultMsgBuilder, RoundResultData>(builder => builder
                    .SetResult(result)
                );
                await Client.SendMessageAsync(notification);
            }
            catch (Exception ex)
            {
                Logger?.Invoke($"[OnlinePlayer]: Ошибка при отправке уведомления о результате раунда: {ex.Message}");
            }
        }

        public override async Task OnTieOffered()
        {
            try
            {
                var notification = _msgService.BuildMessage<TieGameRequestMsgBuilder, TieGameRequestData>();
                await Client.SendMessageAsync(notification);
            }
            catch (Exception ex)
            {
                Logger?.Invoke($"[OnlinePlayer]: Ошибка при отправке уведомления о предложении ничьей: {ex.Message}");
            }
        }

        private char ConvertMoveToChar(int move)
        {
            return move switch
            {
                0 => 'R', // Камень
                1 => 'P', // Бумага
                2 => 'S', // Ножницы
                _ => '?'
            };
        }
    }
}
