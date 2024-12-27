
using System.Collections.Concurrent;
using TopNetwork.Core;

namespace TopNetwork.Services
{
    // Сервис для управления аутентификацией
    public class AuthenticationService
    {
        private readonly UserService _userService;
        private readonly ConcurrentDictionary<TopClient, (string login, DateTime timestamp)> _authenticatedClients = new();

        public TimeSpan MaxSessionDuration { get; set; } = TimeSpan.FromSeconds(30);

        public AuthenticationService(UserService userService)
        {
            _userService = userService;
        }

        public async Task<bool> VerifyAuthenticatedConnection(TopClient client)
        {
            if (_authenticatedClients.TryGetValue(client, out var session))
            {
                if (DateTime.Now - session.timestamp < MaxSessionDuration)
                    return true;

                _authenticatedClients.TryRemove(client, out _);
                return false;
            }           

            return false;
        }

        //public async Task<Message?> HandleAuthenticationRequest(TopClient client, Message message)
        //{
        //    var credentials = message.Payload.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        //    if (credentials.Length != 2)
        //        return CurrencyMsgBuilder.CreateAuthenticationResult(false, "Неверный формат данных. Используйте: '<LOGIN> <PASSWORD>'");

        //    var (login, password) = (credentials[0], credentials[1]);
        //    var users = _userService.GetAllUsers();

        //    if (users.TryGetValue(login, out var storedPassword) && storedPassword == password)
        //    {
        //        _authenticatedClients[client] = (login, DateTime.Now);
        //        return CurrencyMsgBuilder.CreateAuthenticationResult(true, "Аутентификация успешна");
        //    }

        //    return CurrencyMsgBuilder.CreateAuthenticationResult(false, "Неверный логин или пароль.");
        //}

        //public async Task<Message?> HandleCloseSessionRequest(TopClient client)
        //{
        //    _authenticatedClients.TryRemove(client, out _);
        //    client.Close();
        //    return null;
        //}

        //public async Task VerifyAllAuthenticatedConnections()
        //{
        //    foreach (var client in _authenticatedClients.Keys)
        //    {
        //        await VerifyAuthenticatedConnection(client);
        //    }
        //}
    }
}
