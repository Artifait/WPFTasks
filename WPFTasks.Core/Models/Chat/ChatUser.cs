
using TopNetwork.Services;

namespace WPFTasks.Core.Models.Chat
{
    public class ChatUser : User
    {
        public ChatUser(string login, string passwordHash) : base(login, passwordHash)
        {
        }
    }
}
