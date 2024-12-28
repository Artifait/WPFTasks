
using TopNetwork.Core;

namespace TopNetwork.Services.MessageBuilder
{
    public class MessageBuilderService
    {
        private readonly Dictionary<string, Func<object>> _builders = new();

        public MessageBuilderService Register<TMsgSourceData>(Func<IMessageBuilder<TMsgSourceData>> builderFactory)
            where TMsgSourceData : IMsgSourceData
        {
            _builders[typeof(TMsgSourceData).Name] = () => builderFactory();
            return this;
        }

        public IMessageBuilder<TMsgSourceData> CreateBuilder<TMsgSourceData>()
            where TMsgSourceData : IMsgSourceData
        {
            var type = typeof(TMsgSourceData).Name;
            if (_builders.TryGetValue(type, out var value))
            {
                return (IMessageBuilder<TMsgSourceData>)value();
            }

            throw new InvalidOperationException($"No builder registered for type {type}");
        }

        /// <summary>
        /// Упрощает создание и настройку сообщений через MessageBuilderService с преобразованием билдера к конкретному типу.
        /// </summary>
        public TBuilder CreateBuilder<TBuilder, TMsgSourceData>(
            Action<TBuilder>? configure = null)
            where TBuilder : IMessageBuilder<TMsgSourceData>
            where TMsgSourceData : IMsgSourceData
        {
            // Получаем универсальный билдер и приводим его к ожидаемому типу
            var builder = (TBuilder)CreateBuilder<TMsgSourceData>();

            // Применяем дополнительную конфигурацию, если указано
            configure?.Invoke(builder);

            return builder;
        }

        /// <summary>
        /// Упрощает создание сообщения через MessageBuilderService с настройкой билдера.
        /// </summary>
        public Message BuildMessage<TBuilder, TMsgSourceData>(
            Action<TBuilder>? configure = null)
            where TBuilder : IMessageBuilder<TMsgSourceData>
            where TMsgSourceData : IMsgSourceData
        {
            // Получаем и настраиваем билдер, а затем создаём сообщение
            var builder = CreateBuilder<TBuilder, TMsgSourceData>(configure);
            return builder.BuildMsg();
        }
    }
}
