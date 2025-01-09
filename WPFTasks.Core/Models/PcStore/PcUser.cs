
using TopNetwork.Services;

namespace WPFTasks.Core.Models.PcStore
{
    public class PcUser : User
    {
        public static TimeSpan TimeWindow { get; set; } = TimeSpan.FromHours(1);
        public static TimeSpan Cooldown { get; set; } = TimeSpan.FromMinutes(1);
        public static int MaxRequests { get; set; } = 3;

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

            // Отбираем запросы, попадающие в заданное временное окно
            var recentRequests = PcRequestsTimestamps.Where(timestamp => timestamp >= now - TimeWindow).ToList();

            // Если количество запросов превышает лимит
            if (recentRequests.Count >= MaxRequests)
            {
                // Проверяем время самого нового запроса в этом окне
                DateTime firstRequestInWindow = recentRequests[recentRequests.Count - 1];

                // Если с момента первого запроса в окне прошло меньше кулдауна, пользователь невалиден
                if (now - firstRequestInWindow < Cooldown)
                    return false;
            }

            return true;
        }
    }
}
