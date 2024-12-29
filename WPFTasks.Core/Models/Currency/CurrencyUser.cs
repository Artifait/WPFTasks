
using TopNetwork.Services;

namespace WPFTasks.Core.Models.Currency
{
    public class CurrencyUser : User
    {
        public static TimeSpan TimeWindow { get; set; } = TimeSpan.FromMinutes(10);
        public static TimeSpan Cooldown { get; set; } = TimeSpan.FromMinutes(1);
        public static int MaxRequests { get; set; } = 5;

        public List<DateTime> CurrencyRequestsTimestamps { get; set; }

        public CurrencyUser(string login, string passwordHash) : base(login, passwordHash)
        {
            CurrencyRequestsTimestamps = [];
        }
        public void AddCurrencyRequest()
            => CurrencyRequestsTimestamps.Add(DateTime.Now);

        public override async Task<bool> IsUserLoginPossibleAsync()
        {
            DateTime now = DateTime.Now;

            // Отбираем запросы, попадающие в заданное временное окно
            var recentRequests = CurrencyRequestsTimestamps.Where(timestamp => timestamp >= now - TimeWindow).ToList();

            // Если количество запросов превышает лимит
            if (recentRequests.Count >= MaxRequests)
            {
                // Проверяем время самого старого запроса в этом окне
                DateTime firstRequestInWindow = recentRequests.First();

                // Если с момента первого запроса в окне прошло меньше кулдауна, пользователь невалиден
                if (now - firstRequestInWindow < Cooldown)
                    return false;
            }

            return true;
        }
    }
}
