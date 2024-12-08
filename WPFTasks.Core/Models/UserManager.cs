
using System.IO;
using System.Text.Json;
using TopNetwork.Core;
using WPFTasks.Core.Models.Currency;

namespace WPFTasks.Core.Models
{
    public class UserManager
    {
        #region Properties
        /// <summary> Гарант потокобезопасности </summary>
        public object _locker = new();
        /// <summary> Название Файла с сохранениями </summary>
        private string CredentialsFile;
        /// <summary> Зарегистрированный пользователи </summary>
        public Dictionary<string, string> RegisteredUsers { get; private set; } = [];
        /// <summary> Делегат для логирования внутреней работы обьекта </summary>
        public LogString? Logger { get; set; }
        /// <summary> 
        /// Это хранилище проверенных соединений, если с момента начала пройдет больше, чем <see cref="MaxSessionDuration"/><br/>
        /// при следующей проверке через метод <see cref="CheckAuthenticatedConnection"/> будет отправленно сообщение <br/>
        /// о необходимости пройти аутентификацию.
        /// </summary>
        public Dictionary<TopClient, (string login, DateTime timestamp)> AuthenticatedConnection { get; private set; } = [];
        
        public TimeSpan MaxSessionDuration { get; private set; } = TimeSpan.FromSeconds(30);
        #endregion
        public UserManager(string credentialsFile) 
        {
            CredentialsFile = credentialsFile;
        }
        public void AddUser(string login, string password)
        {
            lock (_locker)
            {
                RegisteredUsers[login] = password;
                SaveCredentials();
                Logger?.Invoke($"Добавлен пользователь: {login}");
            }
        }

        public void RemoveUser(string login)
        {
            lock (_locker)
            {
                if (RegisteredUsers.Remove(login))
                {
                    SaveCredentials();
                    Logger?.Invoke($"Удален пользователь: {login}");
                }
                else
                {
                    Logger?.Invoke($"Пользователь не найден: {login}");
                }
            }
        }

        public void LoadCredentials()
        {
            lock (_locker)
            {
                if (File.Exists(CredentialsFile))
                {
                    string json = File.ReadAllText(CredentialsFile);
                    RegisteredUsers = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
                    Logger?.Invoke("Данные пользователей загружены.");
                }
                else
                {
                    Logger?.Invoke("Файл с данными пользователей не найден.");
                }
            }
        }

        public void SaveCredentials()
        {
            string json = JsonSerializer.Serialize(RegisteredUsers, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(CredentialsFile, json);
            Logger?.Invoke("Данные пользователей сохранены.");
        }
        public bool CheckAuthenticatedConnection(TopClient client, string login)
        {
            bool resSearch = AuthenticatedConnection.TryGetValue(client, out var pair) && pair.isAuthenticated && (pair.login == login);
            if (!resSearch)
                return false;

            if(DateTime.Now - pair.timestamp >= MaxSessionDuration)
            {
                
            }

        }
        public async Task<Message?> HandleDisconnection(TopClient client, Message message)
        {
            if(VerifyAuthenticatedConnection(client))
                AuthenticatedConnection.Remove(client);
            
            return null;
        }
        public async Task<Message?> HandleAuthentication(TopClient client, Message message)
        {
            string[] credentials = message.Payload.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (credentials.Length != 2)
            {
                return new Message
                {
                    MessageType = CurrencyServer.GetMessageTypeStr(CurrencyServer.MessageType.Error),
                    Headers = { { "Authenticated", "false" } },
                    Payload = "Неверный формат данных. Ожидается: '<LOGIN> <PASSWORD>'"
                };
            }

            string login = credentials[0];
            string password = credentials[1];

            lock (_locker)
            {
                if (RegisteredUsers.TryGetValue(login, out var storedPassword) && storedPassword == password)
                {
                    Logger?.Invoke($"Успешная аутентификация: {login}");
                    AuthenticatedConnection[client] = true;
                    return new Message
                    {
                        MessageType = "Authentication",
                        Headers = { { "Authenticated", "true" } },
                        Payload = "Аутентификация успешна"
                    };
                }
            }

            Logger?.Invoke($"Ошибка аутентификации: {login}");
            return new Message
            {
                MessageType = CurrencyServer.GetMessageTypeStr(CurrencyServer.MessageType.Error),
                Headers = { { "Authenticated", "false" } },
                Payload = "Неверный логин или пароль"
            };
        }
    }
}
