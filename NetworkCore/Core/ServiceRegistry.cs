
using System.Collections.Concurrent;
using System.Reflection;

namespace TopNetwork.Core
{

    [AttributeUsage(AttributeTargets.Property)]
    public class InjectAttribute : Attribute { }

    public class ServiceRegistry
    {
        private readonly ConcurrentDictionary<Type, object> _services = new(); // Регистрация конкретных типов
        private readonly ConcurrentDictionary<Type, Type> _genericRegistrations = new(); // Регистрация generic-типов

        // Регистрация конкретного сервиса
        public ServiceRegistry Register<TService>(TService service) where TService : class
        {
            ArgumentNullException.ThrowIfNull(service);

            _services[typeof(TService)] = service;
            return this;
        }

        // Регистрация generic-типа
        public ServiceRegistry RegisterGeneric(Type serviceType, Type implementationType)
        {
            if (!serviceType.IsGenericTypeDefinition || !implementationType.IsGenericTypeDefinition)
            {
                throw new ArgumentException("Оба типа должны быть определениями generic-типов.");
            }

            _genericRegistrations[serviceType] = implementationType;
            return this;
        }

        // Получение зарегистрированного сервиса
        public TService? Get<TService>() where TService : class
        {
            return (TService?)GetService(typeof(TService));
        }

        // Получение сервиса по типу
        private object? GetService(Type serviceType)
        {
            if (_services.TryGetValue(serviceType, out var service))
            {
                return service;
            }

            if (serviceType.IsGenericType)
            {
                service = ResolveGenericType(serviceType);
                if (service != null)
                {
                    _services[serviceType] = service;
                    InjectDependencies(service);
                    return service;
                }
            }

            if (serviceType.IsInterface || serviceType.IsAbstract)
            {
                throw new InvalidOperationException($"Нет зарегистрированной реализации для {serviceType.FullName}");
            }

            service = CreateInstance(serviceType);
            if (service != null)
            {
                _services[serviceType] = service;
                InjectDependencies(service);
            }

            return service;
        }

        // Метод TryGetService
        public bool TryGetService<TService>(out TService? service) where TService : class
        {
            service = (TService?)TryGetService(typeof(TService));
            return service != null;
        }

        private object? TryGetService(Type serviceType)
        {
            try
            {
                return GetService(serviceType);
            }
            catch
            {
                return null;
            }
        }

        // Разрешение generic-типа
        private object? ResolveGenericType(Type genericType)
        {
            var genericTypeDefinition = genericType.GetGenericTypeDefinition();

            if (!_genericRegistrations.TryGetValue(genericTypeDefinition, out var implementationType))
            {
                throw new InvalidOperationException($"Generic-тип {genericType.FullName} не зарегистрирован.");
            }

            var constructedType = implementationType.MakeGenericType(genericType.GenericTypeArguments);
            return CreateInstance(constructedType);
        }

        // Создание экземпляра сервиса
        private object? CreateInstance(Type serviceType)
        {
            var constructor = serviceType.GetConstructors()
                .OrderByDescending(c => c.GetParameters().Length)
                .FirstOrDefault();

            if (constructor == null)
            {
                return Activator.CreateInstance(serviceType);
            }

            var parameters = constructor.GetParameters()
                .Select(p => GetService(p.ParameterType) ?? throw new InvalidOperationException(
                    $"Невозможно разрешить зависимость {p.ParameterType.FullName} для {serviceType.FullName}"))
                .ToArray();

            return constructor.Invoke(parameters);
        }

        // Внедрение зависимостей в свойства
        private void InjectDependencies(object instance)
        {
            var properties = instance.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite && p.GetCustomAttribute<InjectAttribute>() != null);

            foreach (var property in properties)
            {
                var dependency = GetService(property.PropertyType) ?? throw new InvalidOperationException(
                    $"Невозможно разрешить зависимость для свойства {property.Name} в {instance.GetType().FullName}");

                property.SetValue(instance, dependency);
            }
        }
    }
}
