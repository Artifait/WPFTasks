
using System.IO;
using System.Text.Json;
using TopNetwork.Core;
using WPFTasks.Core.Models.Currency;

namespace WPFTasks.Core.Models
{
    public class UserManager
    {
        #region Properties
        public object _locker = new();
        /// <summary> Название Файла с сохранениями </summary>
        private string CredentialsFile;
        /// <summary> Зарегистрированный пользователи </summary>
        public Dictionary<string, string> RegisteredUsers { get; private set; } = [];
        /// <summary> Делегат для логирования внутреней работы обьекта </summary>
        public LogString? Logger { get; set; }
        /// <summary> 
        /// Это хранилище проверенных соединений, если с момента начала пройдет больше, чем <see cref="MaxSessionDuration"/><br/>
        /// при следующей проверке через метод <see cref="VerifyAuthenticatedConnection"/> будет отправленно сообщение <br/>
        /// о необходимости пройти аутентификацию.
        /// </summary>
        public Dictionary<TopClient, (string login, DateTime timestamp)> AuthenticatedConnection { get; private set; } = [];
        
        public TimeSpan MaxSessionDuration { get; private set; } = TimeSpan.FromSeconds(30);
        #endregion

        public UserManager(string credentialsFile) 
        {
            CredentialsFile = credentialsFile;
        }

        #region DataBase Logic
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
                    RegisteredUsers = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
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
            string json;
            lock (_locker)
            {
                json = JsonSerializer.Serialize(RegisteredUsers, new JsonSerializerOptions { WriteIndented = true });
            }
            File.WriteAllText(CredentialsFile, json);
            Logger?.Invoke("Данные пользователей сохранены.");
        }
        #endregion

        /// <summary>
        /// 1) Всё ок: True
        /// 2) Если есть, но время сесии кончилось: отправка сообщения об необходимости повторить аутентификацию + отключение соединения + False <br/>
        /// 3) Иначе: False <br/>
        /// </summary>
        public async Task<bool> VerifyAuthenticatedConnection(TopClient client)
        {
            try
            {
                if (AuthenticatedConnection.TryGetValue(client, out var res))
                {
                    if (DateTime.Now - res.timestamp < MaxSessionDuration)
                        return true;

                    await client.SendMessageAsync(CurrencyMsgBuilder.CreateEndSessionNotification());
                    Logger?.Invoke($"{client.RemoteEndPoint}: закончилось время сессии.");
                    client.Close();
                }

                await client.SendMessageAsync(CurrencyMsgBuilder.CreateAuthenticationResult(false, "Пройдите аутентификацию, перед началом использования."));
                return false;
            }
            catch (Exception ex)
            {
                Logger?.Invoke(ex.Message);
                return false;
            }
        }

        public async Task VerifyAllAuthenticatedConnection()
        {
            foreach(var user in AuthenticatedConnection.Keys)
            {
                await VerifyAuthenticatedConnection(user);
            }
        }
        public async Task<Message?> HandleCloseSessionRequest(TopClient client, Message message)
        {
            AuthenticatedConnection.Remove(client);
            client.Close();
            Logger?.Invoke($"{client.RemoteEndPoint}: запрос на закрытие сессии обработан.");
            return null;
        }
        public async Task<Message?> HandleAuthenticationRequest(TopClient client, Message message)
        {
            Logger?.Invoke($"{client.RemoteEndPoint}: Request");

            string[] credentials = message.Payload.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (credentials.Length != 2)
            {
                Logger?.Invoke($"{client.RemoteEndPoint}: Ошибка аутентификации - неверный формат данных.");
                return CurrencyMsgBuilder.CreateAuthenticationResult(false, "Неверный формат данных. Ожидается: '<LOGIN> <PASSWORD>'");
            }
            

            string login = credentials[0];
            string password = credentials[1];

            lock (_locker)
            {
                if (RegisteredUsers.TryGetValue(login, out var storedPassword) && storedPassword == password)
                {
                    Logger?.Invoke($"{client.RemoteEndPoint}: Успешная аутентификация, под логином - {login}.");
                    AuthenticatedConnection[client] = new(login, DateTime.Now);
                    return CurrencyMsgBuilder.CreateAuthenticationResult(true, "Аутентификация успешна");
                }
            }

            Logger?.Invoke($"{client.RemoteEndPoint}: Ошибка аутентификации - неверный логин или пароль.");
            return CurrencyMsgBuilder.CreateAuthenticationResult(false, "Неверный логин или пароль.");
        }
    }
}
