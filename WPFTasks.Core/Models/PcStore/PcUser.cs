
using TopNetwork.Services;

namespace WPFTasks.Core.Models.PcStore
{
    public class PcUser : User
    {
        public static TimeSpan TimeWindow { get; set; } = TimeSpan.FromHours(1);
        public int MaxRequests { get; set; } = 3;

        public List<DateTime> PcRequestsTimestamps { get; set; }

        public PcUser(string login, string passwordHash) : base(login, passwordHash)
        {
            PcRequestsTimestamps = [];
        }
        public void AddPcInfoRequest()
            => PcRequestsTimestamps.Add(DateTime.Now);

        public override async Task<bool> IsUserLoginPossibleAsync()
        {
            DateTime now = DateTime.Now;

            var countRequests = PcRequestsTimestamps.Where(timestamp => timestamp >= now - TimeWindow).Count();

            if (countRequests >= MaxRequests)
                return false;

            return true;
        }
    }
}
