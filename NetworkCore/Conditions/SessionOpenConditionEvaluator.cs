
using TopNetwork.Core;

namespace TopNetwork.Conditions
{
    public class SessionOpenConditionEvaluator : ConditionEvaluator<ClientSession>
    {
        public override SessionOpenConditionEvaluator AddCondition(ICondition<ClientSession> condition)
            => (SessionOpenConditionEvaluator)base.AddCondition(condition);
        public override SessionOpenConditionEvaluator AddAsyncCondition(IAsyncCondition<ClientSession> asyncCondition)
            => (SessionOpenConditionEvaluator)base.AddAsyncCondition(asyncCondition);

        public async Task<bool> ShouldOpenAsync(ClientSession session)
            => await AllConditionsSatisfiedAsync(session);
        public bool ShouldOpen(ClientSession session)               
            => AllConditionsSatisfied(session);
    }
}
