
using TopNetwork.Core;
using TopNetwork.RequestResponse;

namespace TopNetwork.Defaults
{
    public class AuthenticatedClientSession : ClientSession
    {
        public AuthenticatedClientSession(TopClient client, RrServerHandlerBase messageHandler, ServiceRegistry context)
            : base(client, messageHandler, context) { }


        public override async Task StartAsync()
        {
            logger?.Invoke($"[{RemoteEndPoint}]: Authenticating client...");
            // Например, проверка токена или идентификатора
            if (!await AuthenticateClientAsync())
            {
                logger?.Invoke($"[{RemoteEndPoint}]: Authentication failed!");
                CloseSession();
                return;
            }

            logger?.Invoke($"[{RemoteEndPoint}]: Authentication successful!");
            await base.StartAsync();
        }

        private async Task<bool> AuthenticateClientAsync()
        {
            // Реализация аутентификации клиента
            await Task.Delay(100); // Симуляция запроса
            return true; // Допустим, аутентификация всегда успешна
        }
    }
}
