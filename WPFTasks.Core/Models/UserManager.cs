
using System.IO;
using System.Text.Json;
using TopNetwork.Core;

namespace WPFTasks.Core.Models
{
    public class UserManager
    {
        private string CredentialsFile;
        public object _lockerUserCredentials = new();
        public Dictionary<string, string> UserCredentials { get; private set; } = [];
        public LogString? Logger { get; set; }

        public UserManager(string credentialsFile) 
        {
            CredentialsFile = credentialsFile;
        }
        public void AddUser(string login, string password)
        {
            lock (_lockerUserCredentials)
            {
                UserCredentials[login] = password;
                SaveCredentials();
                Logger?.Invoke($"Добавлен пользователь: {login}");
            }
        }

        public void RemoveUser(string login)
        {
            lock (_lockerUserCredentials)
            {
                if (UserCredentials.Remove(login))
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
            lock (_lockerUserCredentials)
            {
                if (File.Exists(CredentialsFile))
                {
                    string json = File.ReadAllText(CredentialsFile);
                    UserCredentials = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
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
            string json = JsonSerializer.Serialize(UserCredentials, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(CredentialsFile, json);
            Logger?.Invoke("Данные пользователей сохранены.");
        }

        public async Task<Message?> HandleAuthentication(TopClient client, Message message, Dictionary<TopClient, bool> authenticatedConnection)
        {
            string[] credentials = message.Payload.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (credentials.Length != 2)
            {
                return new Message
                {
                    MessageType = "Error",
                    Payload = "Неверный формат данных. Ожидается: '<LOGIN> <PASSWORD>'"
                };
            }

            string login = credentials[0];
            string password = credentials[1];

            lock (_lockerUserCredentials)
            {
                if (UserCredentials.TryGetValue(login, out var storedPassword) && storedPassword == password)
                {
                    Logger?.Invoke($"Успешная аутентификация: {login}");
                    authenticatedConnection[client] = true;
                    return new Message
                    {
                        MessageType = "Authentication",
                        Payload = "Аутентификация успешна"
                    };
                }
            }

            Logger?.Invoke($"Ошибка аутентификации: {login}");
            return new Message
            {
                MessageType = "Error",
                Payload = "Неверный логин или пароль"
            };
        }
    }
}
