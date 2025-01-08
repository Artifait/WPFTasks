
using TopNetwork.Conditions;
using TopNetwork.Core;
using TopNetwork.Services;

namespace WPFTasks.Core.Models.Currency.Conditions
{
    public class MaxRequestsCloseCondition : ICondition<ClientSession>
    {
        public int MaxRequests { get; set; } = 3;
        public string MsgType { get; set; } = string.Empty;

        public bool IsSatisfied(ClientSession session)
        {
            if(session.ProcessedMessagesCountOfType.TryGetValue(MsgType, out var count))
                return count >= MaxRequests;

            return false;
        }
    }
}
