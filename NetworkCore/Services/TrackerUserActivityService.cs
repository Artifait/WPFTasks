
using System.Collections.Concurrent;
using TopNetwork.Core;
using TopNetwork.RequestResponse;
using TopNetwork.Services.MessageBuilder;

namespace TopNetwork.Services
{
    public class TrackerUserActivityService
    {
        public ConcurrentDictionary<TopClient, ClientActivitySession> _trackingConnections = [];
        private TimeSpan _maxDurationInactive = TimeSpan.FromMinutes(3);
        private readonly SemaphoreSlim _sessionsLock = new(1, 1);
        private readonly MessageBuilderService _msgService;

        public LogString? Logger { get; set; }
        public TimeSpan MaxDurationInactive => _maxDurationInactive;

        public TrackerUserActivityService(MessageBuilderService msgService)
        {
            _msgService = msgService;
        }

        public void UpdateLastActive(TopClient client)
        {
            if(_trackingConnections.TryGetValue(client, out var session)) {
                session.Restart(_maxDurationInactive);
            }
            else {
                RegisterUser(client);
            }
        }

        public async Task UpdateMaxDurationInactive(TimeSpan newDuration)
        {
            await _sessionsLock.WaitAsync();

            try
            {
                _maxDurationInactive = newDuration;
                Logger?.Invoke($"[AuthenticationService]: Время длительности сессии обновлено до {_maxDurationInactive.TotalMinutes} минут.");

                foreach (var session in _trackingConnections.Values)
                {
                    session.UpdateDuration(newDuration);
                }
            }
            finally
            {
                _sessionsLock.Release();
            }

        }
        private void RegisterUser(TopClient client)
        {
            var session = new ClientActivitySession(client, _maxDurationInactive, NotifySessionExpired);
            client.OnConnectionLost += () => CloseSession(client);
            _trackingConnections[client] = session;
        }

        public void CloseSession(TopClient client)
        {
            if (_trackingConnections.TryRemove(client, out var session))
            {
                session.Dispose();
                Logger?.Invoke($"[TrackerUserActivityService]: Сессия клиента [{client.RemoteEndPoint}] закрыта.");
            }
        }

        private async Task NotifySessionExpired(TopClient client)
        {
            CloseSession(client);
            if (client.IsConnected)
            {
                try { 
                    await client.SendMessageAsync(_msgService.BuildMessage<EndSessionNotificationMessageBuilder, EndSessionNotificationData>(builder => builder
                        .SetPayload("Сессия закрыта, из-за неактивности...")));

                    Logger?.Invoke($"[TrackerUserActivityService]: Клиент [{client.RemoteEndPoint}] уведомлен об закрытии сессии из-за неактивности.");
                }
                catch (Exception ex) {
                    Logger?.Invoke($"[AuthenticationService]: Errore - {ex.Message}");
                }

            }
            client.Disconnect();
        }
    }


    public class ClientActivitySession
    {
        private readonly TopClient _client;
        private readonly Timer _timer;
        private readonly Func<TopClient, Task> _onSessionExpired;
        private readonly object _lock = new(); 

        private DateTime _startTime;
        private TimeSpan _remainingDuration; 
        private bool _isDisposed; 

        public ClientActivitySession(TopClient client, TimeSpan duration, Func<TopClient, Task> onSessionExpired)
        {
            _client = client;
            _onSessionExpired = onSessionExpired;
            _remainingDuration = duration;
            _startTime = DateTime.UtcNow;

            _timer = new Timer(OnTimerElapsed, null, duration, Timeout.InfiniteTimeSpan);
        }

        public void Restart(TimeSpan newDuration)
        {
            lock (_lock)
            {
                if (_isDisposed)
                    return;

                _remainingDuration = newDuration;
                _startTime = DateTime.UtcNow;
                _timer.Change(newDuration, Timeout.InfiniteTimeSpan);
            }
        }

        public void UpdateDuration(TimeSpan newDuration)
        {
            lock (_lock)
            {
                if (_isDisposed)
                    return;

                var elapsedTime = DateTime.UtcNow - _startTime;

                if (elapsedTime >= newDuration)
                {
                    TriggerExpiration();
                }
                else
                {
                    _remainingDuration = newDuration - elapsedTime;
                    _startTime = DateTime.UtcNow;
                    _timer.Change(_remainingDuration, Timeout.InfiniteTimeSpan);
                }
            }
        }

        private void TriggerExpiration()
        {
            Dispose();
            _ = _onSessionExpired(_client);
        }

        private async void OnTimerElapsed(object? state)
        {
            lock (_lock)
            {
                if (_isDisposed)
                    return;

                _isDisposed = true; 
            }

            await _onSessionExpired(_client);
        }

        public void Dispose()
        {
            lock (_lock)
            {
                if (_isDisposed)
                    return;

                _isDisposed = true;
                _timer.Dispose();
            }
        }
    }
}
