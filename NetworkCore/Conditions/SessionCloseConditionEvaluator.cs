
using TopNetwork.Core;

namespace TopNetwork.Conditions
{
    public class SessionCloseConditionEvaluator : ConditionEvaluator<ClientSession>
    {
        public async Task<bool> ShouldCloseAsync(ClientSession session)
        {
            return await AnyConditionSatisfiedAsync(session);
        }

        public bool ShouldClose(ClientSession session)
        {
            return AnyConditionSatisfied(session);
        }
    }
}
