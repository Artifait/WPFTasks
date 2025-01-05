
using TopNetwork.Conditions;
using TopNetwork.Core;
using TopNetwork.Services.MessageBuilder;
using TopNetwork.Services;
using WPFTasks.Core.Models.Currency.MessageBuilder;

namespace WPFTasks.Core.Models.Currency.Conditions
{
    public class ConnectionLimitCondition : IAsyncCondition<ClientSession>
    {
        public int MaxConnections { get; set; } = 2;

        public async Task<bool> IsSatisfiedAsync(ClientSession session)
        {
            if (session.ServerContext.TryGetService<AuthenticationService<CurrencyUser>>(out var authService))
            {
                await authService.VerifyAllSessions();
                if (authService.CountConnections < MaxConnections)
                    return true;

                if (session.ServerContext.TryGetService<MessageBuilderService>(out var msgBuilder))
                {
                    try
                    {
                        await session.SendMessage(msgBuilder.BuildMessage<ServerOverloadedNotificationMessageBuilder, ServerOverloadedNotificationData>(null));
                    }
                    catch (Exception ex)
                    {
                        session.logger?.Invoke($"[CurrencyOpenCondition]: {ex.Message}.");
                    }

                    return false;
                }

                session.logger?.Invoke($"[ServerContext]: Чееел ты забыл зарегать сервис - {nameof(MessageBuilderService)}");
                return false;
            }

            session.logger?.Invoke($"[ServerContext]: Чееел ты забыл зарегать сервис - {nameof(AuthenticationService<CurrencyUser>)}");
            return false;
        }
    }
}
