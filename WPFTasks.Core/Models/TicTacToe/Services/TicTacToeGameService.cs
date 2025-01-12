
using System.Collections.Concurrent;
using TopNetwork.Core;
using TopNetwork.RequestResponse;
using TopNetwork.Services.MessageBuilder;
using WPFTasks.Core.Models.TicTacToe.MessageBuilder;
using WPFTasks.Core.Models.TicTacToe.Services.TicTacToeLogic;
using WPFTasks.Core.Models.TicTacToe.Services.TicTacToeLogic.GameCores;

namespace WPFTasks.Core.Models.TicTacToe.Services
{
    public class TicTacToeGameService
    {
        private readonly MessageBuilderService _msgService = new MessageBuilderService()
            .Register(() => new ErroreMessageBuilder());

        private readonly ConcurrentDictionary<TopClient, TicTacToeSession> _sessions = new();
        private readonly object _locker = new();
        private TopClient? _finderGameUserAndUser;
        public LogString? Logger { get; set; }

        public async Task FindGameToClient(TopClient client, FindGameRequestData requestData)
        {
            if(_sessions.TryGetValue(client, out var session))
            {
                var response = _msgService.BuildMessage<ErroreMessageBuilder, ErroreData>(builder => builder
                    .SetPayload("Отказоно в запуске поиске игры, тк вы уже находитесь в игре...")
                );
                await client.SendMessageAsync(response);
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
            TopClient? playerX = null;
            lock (_locker)
            {
                if (_finderGameUserAndUser == null || !_finderGameUserAndUser.IsConnected)
                {
                    _finderGameUserAndUser = client;
                    return;
                }

                playerX = _finderGameUserAndUser;
                _finderGameUserAndUser = null;
            }

            var playerO = client;
            var session = new TicTacToeSession();
            session.InitHHGame(playerX, playerO);

            _sessions.TryAdd(playerX, session);
            _sessions.TryAdd(playerO, session);

            session.SessionClosed += OnSessionClosed;
            await session.StartGame(CancellationToken.None);
        }

        private async Task HandleHumanComputerGame(TopClient client)
        {
            var session = new TicTacToeSession();
            session.InitHCGame(client);
            _sessions.TryAdd(client, session);
            await session.StartGame(CancellationToken.None);
        }

        private async Task HandleComputerComputerGame(TopClient client)
        {
            var session = new TicTacToeSession();
            _sessions.TryAdd(client, session);
            await session.StartGame(CancellationToken.None);
        }

        private async void OnSessionClosed(TicTacToeSession session)
        {
            // Удаляем сессия чтоб пользователя могли снова искать новые игры
            switch(session.GameType)
            {
                case GameTypes.Human_Human:
                    _sessions.TryRemove(((HHGameCore)session.GameCore).PlayerX.Client, out _);
                    _sessions.TryRemove(((HHGameCore)session.GameCore).PlayerO.Client, out _);
                    break;
                case GameTypes.Human_Computer:
                    _sessions.TryRemove(((HCGameCore)session.GameCore).PlayerX.Client, out _);
                    break;
                case GameTypes.Computer_Computer:
                    _sessions.TryRemove(((CCGameCore)session.GameCore).Initiator, out _);
                    break;
            }
        }
    }

    public class TicTacToeSession
    {
        private readonly CancellationTokenSource _cts = new();

        public event Action<TicTacToeSession>? SessionClosed;
        public GameTypes GameType { get; private set; }
        public TicTacToeGame GameCore { get; private set; }

        public void InitHHGame(TopClient playerX, TopClient playerO)
        {
            GameCore = new HHGameCore(playerX, playerO);
            GameType = GameTypes.Human_Human;
        }

        public void InitHCGame(TopClient playerX)
        {
            GameCore = new HCGameCore(playerX);
            GameType = GameTypes.Human_Computer;
        }

        public void InitCCGame(TopClient initiator)
        {
            GameCore = new CCGameCore(initiator);
            GameType = GameTypes.Computer_Computer;
        }

        public async Task StartGame(CancellationToken token)
        {
            if (GameCore == null)
                throw new Exception("Plz Init core");

            try
            {
                await GameCore.Start(token);
            }
            catch (OperationCanceledException)
            {
                // Handle game cancellation.
            }
            finally
            {
                SessionClosed?.Invoke(this);
            }
        }
    }
}
