

namespace TopNetwork.Core.Defaults
{
    public class DefaultServerStatus : IServerStatus
    {
        public bool IsRunning { get; set; }
        public DateTime? StartTime { get; set; }
        public int ActiveConnections { get; set; }
        public long TotalRequestsHandled { get; set; }

        public virtual string GetStatusSummary()
        {
            return $"Сервер {(IsRunning ? "работает" : "не запущен")}. " +
                   $"Время начала: {StartTime?.ToString() ?? "N/A"}. " +
                   $"Активные соединения: {ActiveConnections}. " +
                   $"Общее количество обработанных запросов: {TotalRequestsHandled}.";
        }

        public async Task SetDefaultState()
        {
            IsRunning = false;
            StartTime = null;

        }
    }
}
