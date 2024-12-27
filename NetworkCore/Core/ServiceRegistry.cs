
using System.Collections.Concurrent;
using System.Reflection;

namespace TopNetwork.Core
{

    [AttributeUsage(AttributeTargets.Property)]
    public class InjectAttribute : Attribute { }

    /// <summary>
    /// Регистр сервисов, чтоб не плодить статики
    /// </summary>
    public class ServiceRegistry
    {
        private readonly ConcurrentDictionary<Type, object> _services = new();

        // Регистрации сервиса
        public ServiceRegistry Register<TService>(TService service) where TService : class
        {
            ArgumentNullException.ThrowIfNull(service);

            _services[typeof(TService)] = service;
            return this;
        }

        // Проверка, зарегистрирован ли сервис
        public bool IsRegistered<TService>() where TService : class
        {
            return _services.ContainsKey(typeof(TService));
        }

        // Получение сервиса с автоматическим разрешением зависимостей
        public TService? Get<TService>() where TService : class
        {
            return (TService?)GetService(typeof(TService));
        }

        private object? GetService(Type serviceType)
        {
            if (_services.TryGetValue(serviceType, out var service))
            {
                return service;
            }

            // Создаём объект через конструктор (или через активацию по умолчанию)
            service = CreateInstance(serviceType);

            if (service == null)
            {
                throw new InvalidOperationException($"Unable to create an instance of type {serviceType.FullName}");
            }

            // Регистрируем созданный объект
            _services[serviceType] = service;

            // Заполняем зависимости через свойства
            InjectDependencies(service);

            return service;
        }

        private object? CreateInstance(Type serviceType)
        {
            // Пробуем найти конструктор с параметрами
            var constructor = serviceType.GetConstructors()
                .OrderByDescending(c => c.GetParameters().Length)
                .FirstOrDefault();

            if (constructor == null)
            {
                // Если конструкторов нет, используем стандартную активацию
                return Activator.CreateInstance(serviceType);
            }

            // Разрешаем параметры конструктора
            var parameters = constructor.GetParameters();
            var parameterInstances = parameters
                .Select(p => GetService(p.ParameterType))
                .ToArray();

            if (parameterInstances.Any(p => p == null))
            {
                throw new InvalidOperationException($"Cannot resolve dependencies for type {serviceType.FullName}");
            }

            return constructor.Invoke(parameterInstances);
        }

        private void InjectDependencies(object instance)
        {
            var properties = instance.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite && p.GetCustomAttribute<InjectAttribute>() != null);

            foreach (var property in properties)
            {
                var dependency = GetService(property.PropertyType) ?? throw new InvalidOperationException(
                    $"Unable to resolve dependency for property {property.Name} in type {instance.GetType().FullName}");

                property.SetValue(instance, dependency);
            }
        }
    }
}
