
using TopNetwork.Services.MessageBuilder;
using WPFTasks.Core.Models.Core;
using WPFTasks.Core.Models.TicTacToe.MessageBuilder;
using WPFTasks.Core.Models.TicTacToe.Services.TicTacToeLogic;

namespace WPFTasks.Core.Models.TicTacToe
{
    public class TicTacToeClient : BaseClient
    {
        public event Action<EndSessionNotificationData>? OnEndSession;
        public event Action<AuthenticationResponseData>? OnAuthenticationResponse;
        public event Action<UpdateGameBoardData>? OnUpdateGameBoard;
        public event Action<GameEndedData>? OnGameEnded;
        public event Action<GameStartedData>? OnGameStarted;

        protected override void RegisterMessageBuilders()
        {
            MessageBuilderService
                .Register(() => new AuthenticationRequestMessageBuilder())
                .Register(() => new CloseSessionRequestMessageBuilder())
                .Register(() => new FindGameRequestMsgBuilder())    // Найти игру
                .Register(() => new UserMoveMsgBuilder())           // Сделать ход
                .Register(() => new EndGameRequestMsgBuilder());    // Признать поражение
        }

        protected override void RegisterMessageHandlers()
        {
            Handlers
                .AddHandlerForMessageType(EndSessionNotificationData.MsgType, async msg =>
                {
                    Disconnect();
                    try { OnEndSession?.Invoke(EndSessionNotificationMessageBuilder.Parse(msg)); }
                    catch { InvokeOnErroreOnClient($"Error parsing response: {msg}");}
                    return null;
                })
                .AddHandlerForMessageType(AuthenticationResponseData.MsgType, async msg =>
                {
                    try { OnAuthenticationResponse?.Invoke(AuthenticationResponseMessageBuilder.Parse(msg)); }
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
                .AddHandlerForMessageType(UpdateGameBoardData.MsgType, async msg =>
                {
                    try { OnUpdateGameBoard?.Invoke(UpdateGameBoardMsgBuilder.Parse(msg)); }
                    catch { InvokeOnErroreOnClient($"Error parsing response: {msg}"); }
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
                });
        }

        public async Task SendFindGameRequest(GameTypes gameType)
        {
            await SendMessageAsync<FindGameRequestMsgBuilder, FindGameRequestData>(
                builder => builder.SetGameType(gameType)
            );
        }

        public async Task SendUserMove(int cellNumber)
        {
            await SendMessageAsync<UserMoveMsgBuilder, UserMoveData>(
                builder => builder.SetCellNumber(cellNumber)
            );
        }

        public async Task SendEndGameRequest()
        {
            await SendMessageAsync<EndGameRequestMsgBuilder, EndGameRequestData>();
        }

        public async Task SendAuthRequest(string login, string password)
        {
            await SendMessageAsync<AuthenticationRequestMessageBuilder, AuthenticationRequestData>(
                builder => builder.SetLogin(login).SetPassword(password)
            );
        }

        public async Task SendCloseSessionRequest()
        {
            await SendMessageAsync<CloseSessionRequestMessageBuilder, CloseSessionRequestData>();
        }
    }
}
