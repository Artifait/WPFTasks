
using System.Text.Json;

namespace TopNetwork.Services
{
    public class User
    {
        public string Login { get; set; }
        public string PasswordHash { get; set; }
        public DateTime CreatedAt { get; set; }

        public User(string login, string passwordHash)
        {
            Login = login;
            PasswordHash = passwordHash;
            CreatedAt = DateTime.UtcNow;
        }

        // Метод проверяющий возможно ли авторизоваться сейчас под этим пользователем
        public async virtual Task<bool> IsUserLoginPossibleAsync()
        {
            return true;
        }
    }

    public interface IRepository<T> where T : class
    {
        void Add(T entity);
        void Remove(Func<T, bool> predicate);
        T? Get(Func<T, bool> predicate);
        List<T> GetAll();
    }


    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly string _filePath;
        private readonly object _locker = new();

        public Repository(string filePath)
        {
            _filePath = filePath;
        }

        public void Add(T entity)
        {
            lock (_locker)
            {
                var entities = GetAll();
                entities.Add(entity);
                SaveToFile(entities);
            }
        }

        public void Remove(Func<T, bool> predicate)
        {
            lock (_locker)
            {
                var entities = GetAll();
                var entityToRemove = entities.FirstOrDefault(predicate);
                if (entityToRemove != null)
                {
                    entities.Remove(entityToRemove);
                    SaveToFile(entities);
                }
            }
        }

        public T? Get(Func<T, bool> predicate)
        {
            lock (_locker)
            {
                return GetAll().FirstOrDefault(predicate);
            }
        }

        public List<T> GetAll()
        {
            lock (_locker)
            {
                if (!File.Exists(_filePath))
                    return new List<T>();

                var json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
            }
        }

        private JsonSerializerOptions _options = new() { WriteIndented = true };

        private void SaveToFile(List<T> entities)
        {
            var json = JsonSerializer.Serialize(entities, _options);
            File.WriteAllText(_filePath, json);
        }
    }


    public class UserService<UserT> where UserT : User
    {
        private readonly IRepository<UserT> _repository;
        private readonly PasswordService _passwordService;

        public UserService(IRepository<UserT> repository, PasswordService passwordService)
        {
            _repository = repository;
            _passwordService = passwordService;
        }

        public void RegisterUser(UserT user)
        {
            if (string.IsNullOrWhiteSpace(user.Login) || string.IsNullOrWhiteSpace(user.PasswordHash))
                throw new ArgumentException("Логин и пароль не могут быть пустыми.");

            if (_repository.Get(u => u.Login == user.Login) != null)
                throw new InvalidOperationException("Пользователь с таким логином уже существует.");

            _repository.Add(user);
        }

        public UserT? Authenticate(string login, string password)
        {
            var user = _repository.Get(u => u.Login == login);
            if (user != null && _passwordService.VerifyHashedPassword(user.PasswordHash, password))
            {
                return user;
            }

            return null;
        }

        public void RemoveUser(string login)
        {
            _repository.Remove(u => u.Login == login);
        }

        public List<UserT> GetAllUsers()
        {
            return _repository.GetAll();
        }
    }
}
