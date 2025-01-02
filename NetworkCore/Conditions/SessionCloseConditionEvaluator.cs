
using TopNetwork.Core;

namespace TopNetwork.Conditions
{
    public class SessionCloseConditionEvaluator : ConditionEvaluator<ClientSession>
    {
        public override SessionCloseConditionEvaluator AddCondition(ICondition<ClientSession> condition)
            => (SessionCloseConditionEvaluator)base.AddCondition(condition);
        public override SessionCloseConditionEvaluator AddAsyncCondition(IAsyncCondition<ClientSession> asyncCondition)
            => (SessionCloseConditionEvaluator)base.AddAsyncCondition(asyncCondition);

        public async Task<bool> ShouldCloseAsync(ClientSession session)
            => await AnyConditionSatisfiedAsync(session) || AnyConditionSatisfied(session);
        public bool ShouldClose(ClientSession session)
            => AnyConditionSatisfied(session) || AnyConditionSatisfiedAsync(session).Result;
    }
}
