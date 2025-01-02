
namespace TopNetwork.Conditions
{
    public class ConditionEvaluator<T>
    {
        private readonly List<ICondition<T>> _conditions = new();
        private readonly List<IAsyncCondition<T>> _asyncConditions = new();

        public virtual ConditionEvaluator<T> AddCondition(ICondition<T> condition)
        {
            ArgumentNullException.ThrowIfNull(condition);
            _conditions.Add(condition);
            return this;
        }

        public virtual ConditionEvaluator<T> AddAsyncCondition(IAsyncCondition<T> asyncCondition)
        {
            ArgumentNullException.ThrowIfNull(asyncCondition);
            _asyncConditions.Add(asyncCondition);
            return this;
        }

        public bool AnyConditionSatisfied(T context)
            => _conditions.Count == 0 ? false :
               _conditions.Any(condition => condition.IsSatisfied(context));

        public async Task<bool> AnyConditionSatisfiedAsync(T context)
        {
            if (_asyncConditions.Count == 0)
                return false;

            foreach (var condition in _asyncConditions)
            {
                if (await condition.IsSatisfiedAsync(context))
                    return true;
            }
            return false;
        }

        public bool AllConditionsSatisfied(T context)
            => _conditions.Count == 0 ? true :
               _conditions.All(condition => condition.IsSatisfied(context));

        public async Task<bool> AllConditionsSatisfiedAsync(T context)
        {
            if (_asyncConditions.Count == 0)
                return true;

            foreach (var condition in _asyncConditions)
            {
                if (!await condition.IsSatisfiedAsync(context))
                    return false;
            }
            return true;
        }
    }
}
