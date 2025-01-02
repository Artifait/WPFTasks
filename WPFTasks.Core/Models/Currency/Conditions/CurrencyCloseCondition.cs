
using TopNetwork.Conditions;
using TopNetwork.Core;
using TopNetwork.Services;

namespace WPFTasks.Core.Models.Currency.Conditions
{
    public class CurrencyCloseCondition : IAsyncCondition<ClientSession>
    {
        public async Task<bool> IsSatisfiedAsync(ClientSession session)
        {
            if (session.ServerContext.TryGetService<AuthenticationService<CurrencyUser>>(out var authService))
                return !await authService.VerifySession(session.Client);

            session.logger?.Invoke($"[ServerContext]: Чееел ты забыл зарегать сервис - {nameof(AuthenticationService<CurrencyUser>)}");
            return true;
        }
    }
}
