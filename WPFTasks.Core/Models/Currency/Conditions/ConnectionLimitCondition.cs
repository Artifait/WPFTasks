
using TopNetwork.Conditions;
using TopNetwork.Core;
using TopNetwork.Services;

namespace WPFTasks.Core.Models.Currency.Conditions
{
    public class AuthCloseCondition : IAsyncCondition<ClientSession>
    {
        public async Task<bool> IsSatisfiedAsync(ClientSession session)
        {
            if (session.ServerContext.TryGetService<AuthenticationService<CurrencyUser>>(out var authService))
                return !await authService.VerifySession(session.Client);

            session.logger?.Invoke($"[ServerContext]: Чееел ты забыл зарегать сервис - {nameof(AuthenticationService<CurrencyUser>)}");
            return true;
        }
    }

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
