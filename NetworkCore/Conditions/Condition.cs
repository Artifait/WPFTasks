
namespace TopNetwork.Conditions
{
    public interface ICondition<T>
    {
        bool IsSatisfied(T context);
    }

    public interface IAsyncCondition<T>
    {
        Task<bool> IsSatisfiedAsync(T context);
    }
}
