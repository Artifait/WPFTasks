
namespace TopNetwork.Core
{
    public interface IServerStatus
    {
        bool IsRunning { get; set; }
        DateTime? StartTime { get; set; }
        int ActiveConnections { get; set; }
        long TotalRequestsHandled { get; set; }
        string GetStatusSummary();
    }
}
