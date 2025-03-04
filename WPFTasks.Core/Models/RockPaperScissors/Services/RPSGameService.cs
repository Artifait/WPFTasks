using System.Collections.Concurrent;
using TopNetwork.Core;
using TopNetwork.RequestResponse;
using TopNetwork.Services.MessageBuilder;
using WPFTasks.Core.Models.RockPaperScissors.MessageBuilder;
using WPFTasks.Core.Models.RockPaperScissors.Services.RockPaperScissorsLogic;
using WPFTasks.Core.Models.RockPaperScissors.Services.RockPaperScissorsLogic.GameCores;

namespace WPFTasks.Core.Models.RockPaperScissors.Services
{
    public class RPSGameService
    {
        private readonly MessageBuilderService _msgService = new MessageBuilderService()
            .Register(() => new FindGameResponseMsgBuilder());

        private readonly ConcurrentDictionary<TopClient, RPSSession> _sessions = new();
        private readonly object _locker = new();
        private TopClient? _finderGameUserAndUser;
        public LogString? Logger { get; set; }

        public async Task FindGameToClient(TopClient client, FindGameRequestData requestData)
        {
            try
            {
                if (_sessions.ContainsKey(client))
                {
                    var response = _msgService.BuildMessage<FindGameResponseMsgBuilder, FindGameResponseData>(builder => builder
                        .SetSearchState("Отказано в запуске поиска игры, тк вы уже находитесь в игре...")
                    );
                    await client.SendMessageAsync(response);
                    return;
                }
                if (_finderGameUserAndUser == client)
                {
                    var response = _msgService.BuildMessage<FindGameResponseMsgBuilder, FindGameResponseData>(builder => builder
                        .SetSearchState("Отказано в запуске поиска игры, тк вам уже ищется игра...")
                    );
                    await client.SendMessageAsync(response);
                    return;
                }
                if ((int)requestData.GameType < 0 || (int)requestData.GameType > 2)
                {
                    var response = _msgService.BuildMessage<FindGameResponseMsgBuilder, FindGameResponseData>(builder => builder
                        .SetSearchState("Данный режим игры не найден...")
                    );
                    await client.SendMessageAsync(response);
                    return;
                }
            }
            catch (Exception ex)
            {
                Logger?.Invoke($"[RPSGameService]: Ошибка при отказе игроку - [{client.LastUseEndPoint}] в поиске игры. {ex.Message}");
                return;
            }

            try
            {
                var response = _msgService.BuildMessage<FindGameResponseMsgBuilder, FindGameResponseData>(builder => builder
                    .SetSearchState("Поиск игры начался!")
                );
                await client.SendMessageAsync(response);
            }
            catch (Exception ex)
            {
                Logger?.Invoke($"[RPSGameService]: Ошибка при уведомлении игрока [{client.LastUseEndPoint}] что для него запущен поиск игры. {ex.Message}");
                return;
            }

            switch (requestData.GameType)
            {
                case GameTypes.Human_Human:
                    await HandleHumanHumanGame(client);
                    break;
                case GameTypes.Human_Computer:
                    await HandleHumanComputerGame(client);
                    break;
                case GameTypes.Computer_Computer:
                    await HandleComputerComputerGame(client);
                    break;
            }
        }

        private async Task HandleHumanHumanGame(TopClient client)
        {
            TopClient? player1 = null;
            lock (_locker)
            {
                if (_finderGameUserAndUser == null || !_finderGameUserAndUser.IsConnected)
                {
                    _finderGameUserAndUser = client;
                    return;
                }
                player1 = _finderGameUserAndUser;
                _finderGameUserAndUser = null;
            }

            var player2 = client;
            var session = new RPSSession();
            session.InitHHGame(player1, player2);

            _sessions.TryAdd(player1, session);
            _sessions.TryAdd(player2, session);

            session.SessionClosed += OnSessionClosed;
            _ = session.StartGame(CancellationToken.None);
        }

        private async Task HandleHumanComputerGame(TopClient client)
        {
            var session = new RPSSession() { Logger = Logger };
            session.InitHCGame(client);
            _sessions.TryAdd(client, session);

            session.SessionClosed += OnSessionClosed;
            _ = session.StartGame(CancellationToken.None);
        }

        private async Task HandleComputerComputerGame(TopClient client)
        {
            var session = new RPSSession() { Logger = Logger };
            session.InitCCGame(client);
            _sessions.TryAdd(client, session);

            session.SessionClosed += OnSessionClosed;
            _ = session.StartGame(CancellationToken.None);
        }

        private async void OnSessionClosed(RPSSession session)
        {
            switch (session.GameType)
            {
                case GameTypes.Human_Human:
                    _sessions.TryRemove(((HHGameCore)session.GameCore).Player1.Client, out _);
                    _sessions.TryRemove(((HHGameCore)session.GameCore).Player2.Client, out _);
                    break;
                case GameTypes.Human_Computer:
                    _sessions.TryRemove(((HCGameCore)session.GameCore).Player.Client, out _);
                    break;
                case GameTypes.Computer_Computer:
                    _sessions.TryRemove(((CCGameCore)session.GameCore).Initiator, out _);
                    break;
            }
        }
    }

    public class RPSSession
    {
        private readonly CancellationTokenSource _cts = new();

        public event Action<RPSSession>? SessionClosed;
        public GameTypes GameType { get; private set; }
        public RPSGame GameCore { get; private set; }
        public LogString? Logger { get; set; }

        public void InitHHGame(TopClient player1, TopClient player2)
        {
            GameCore = new HHGameCore(player1, player2, Logger);
            GameType = GameTypes.Human_Human;
        }

        public void InitHCGame(TopClient player)
        {
            GameCore = new HCGameCore(player, Logger);
            GameType = GameTypes.Human_Computer;
        }

        public void InitCCGame(TopClient initiator)
        {
            GameCore = new CCGameCore(initiator, Logger);
            GameType = GameTypes.Computer_Computer;
        }

        public async Task StartGame(CancellationToken token)
        {
            if (GameCore == null)
                throw new Exception("Please initialize game core");

            GameCore.OnGameEnded += () => SessionClosed?.Invoke(this);

            try
            {
                await GameCore.Start(token);
            }
            catch (OperationCanceledException)
            {
                // Обработка отмены.
            }
            finally
            {
                SessionClosed?.Invoke(this);
            }
        }
    }
}
