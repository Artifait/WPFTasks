using TopNetwork.Services.MessageBuilder;
using WPFTasks.Core.Models.Core;
using WPFTasks.Core.Models.RockPaperScissors.MessageBuilder;
using WPFTasks.Core.Models.RockPaperScissors.Services.RockPaperScissorsLogic;

namespace WPFTasks.Core.Models.RockPaperScissors
{
    public class RockPaperScissorsClient : BaseClient
    {
        public event Action<EndSessionNotificationData>? OnEndSession;
        public event Action<RoundResultData>? OnRoundResult;
        public event Action<GameEndedData>? OnGameEnded;
        public event Action<GameStartedData>? OnGameStarted;
        public event Action? OnTieOffered;
        public event Action<PlayerTurnData>? OnPlayerTurn;
        public event Action<FindGameResponseData>? OnFindGameResponse;

        protected override void RegisterMessageBuilders()
        {
            MessageBuilderService
                .Register(() => new FindGameRequestMsgBuilder())    // Поиск игры
                .Register(() => new UserMoveMsgBuilder())           // Отправка хода (0,1,2)
                .Register(() => new EndGameRequestMsgBuilder())     // Сдаться / завершить игру
                .Register(() => new TieGameRequestMsgBuilder());    // Предложение ничьей
        }

        protected override void RegisterMessageHandlers()
        {
            Handlers
                .AddHandlerForMessageType(EndSessionNotificationData.MsgType, async msg =>
                {
                    Disconnect();
                    try { OnEndSession?.Invoke(EndSessionNotificationMessageBuilder.Parse(msg)); }
                    catch { InvokeOnErroreOnClient($"Error parsing response: {msg}"); }
                    return null;
                })
                .AddHandlerForMessageType(ErroreData.MsgType, async msg =>
                {
                    try { InvokeOnErroreFromServer(ErroreMessageBuilder.Parse(msg)); }
                    catch { InvokeOnErroreOnClient($"Error parsing response: {msg}"); }
                    return null;
                })
                .AddHandlerForMessageType(ServerOverloadedNotificationData.MsgType, async msg =>
                {
                    InvokeOnServerOverloaded();
                    return null;
                })
                .AddHandlerForMessageType(GameEndedData.MsgType, async msg =>
                {
                    try { OnGameEnded?.Invoke(GameEndedMsgBuilder.Parse(msg)); }
                    catch { InvokeOnErroreOnClient($"Error parsing response: {msg}"); }
                    return null;
                })
                .AddHandlerForMessageType(GameStartedData.MsgType, async msg =>
                {
                    try { OnGameStarted?.Invoke(GameStartedMsgBuilder.Parse(msg)); }
                    catch { InvokeOnErroreOnClient($"Error parsing response: {msg}"); }
                    return null;
                })
                .AddHandlerForMessageType(TieGameRequestData.MsgType, async msg =>
                {
                    OnTieOffered?.Invoke();
                    return null;
                })
                .AddHandlerForMessageType(PlayerTurnData.MsgType, async msg =>
                {
                    try { OnPlayerTurn?.Invoke(PlayerTurnMsgBuilder.Parse(msg)); }
                    catch { InvokeOnErroreOnClient($"Error parsing response: {msg}"); }
                    return null;
                })
                .AddHandlerForMessageType(FindGameResponseData.MsgType, async msg =>
                {
                    try { OnFindGameResponse?.Invoke(FindGameResponseMsgBuilder.Parse(msg)); }
                    catch { InvokeOnErroreOnClient($"Error parsing response: {msg}"); }
                    return null;
                })
                .AddHandlerForMessageType(RoundResultData.MsgType, async msg =>
                {
                    try { OnRoundResult?.Invoke(RoundResultMsgBuilder.Parse(msg)); }
                    catch { InvokeOnErroreOnClient($"Error parsing response: {msg}"); }
                    return null;
                });
        }

        public async Task SendFindGameRequest(GameTypes gameType)
        {
            await SendMessageAsync<FindGameRequestMsgBuilder, FindGameRequestData>(
                builder => builder.SetGameType(gameType)
            );
        }

        public async Task SendUserMove(int move)
        {
            await SendMessageAsync<UserMoveMsgBuilder, UserMoveData>(
                builder => builder.SetMove(move)
            );
        }

        public async Task SendGiveUp()
        {
            await SendMessageAsync<EndGameRequestMsgBuilder, EndGameRequestData>();
        }

        public async Task SendEndGameRequest()
        {
            await SendMessageAsync<EndGameRequestMsgBuilder, EndGameRequestData>();
        }

        public async Task SendTieRequest()
        {
            await SendMessageAsync<TieGameRequestMsgBuilder, TieGameRequestData>();
        }
    }
}
