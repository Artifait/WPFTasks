
using TopNetwork.Core;

namespace TopNetwork.Conditions
{
    public class SessionOpenConditionEvaluator : ConditionEvaluator<ClientSession>
    {
        public async Task<bool> ShouldOpenAsync(ClientSession session)
        {
            return await AllConditionsSatisfiedAsync(session);
        }

        public bool ShouldOpen(ClientSession session)               
        {
            return AllConditionsSatisfied(session);
        }
    }
}
