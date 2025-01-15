
using TopNetwork.Services;

namespace WPFTasks.Core.Models.Chat
{
    public class ChatUser : User
    {
        public DateTime DateEndBan { get; set; } = DateTime.MinValue;
        public int CountBadWords { get; set; } = 0;

        public ChatUser(string login, string passwordHash) : base(login, passwordHash)
        {
        }

        public string GetStatistics()
        {
            return "{\n" +
                    $"    CountBadWords: {CountBadWords};\n" +
                    (DateEndBan <= DateTime.UtcNow ? string.Empty : $"    DateEndBan: {DateEndBan}\n") +
                    "}";
        }
    }
}
