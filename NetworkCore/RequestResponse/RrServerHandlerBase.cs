
using System.Collections.Concurrent;
using TopNetwork.Core;

namespace TopNetwork.RequestResponse
{
    public class RrServerHandlerBase
    {
        public ConcurrentDictionary<string, Func<TopClient, Message, ServiceRegistry, Task<Message?>>> HandlerOfMessageType { get; private set; }
        public Func<TopClient, Message, ServiceRegistry, Task<Message?>> DefaultHandler { get; private set; }

        public RrServerHandlerBase()
        {
            DefaultHandler = DefaultHandlerRealization;
            HandlerOfMessageType = new();
        }

        public async Task<Message?> HandleMessage(TopClient client, Message msg, ServiceRegistry context)
        {
            string msgType = msg.MessageType;

            if (HandlerOfMessageType.TryGetValue(msgType, out var func))
                return await func(client, msg, context);

            return await DefaultHandler(client, msg, context);
        }

        public RrServerHandlerBase AddHandlerForMessageType(string type, Func<TopClient, Message, ServiceRegistry, Task<Message?>> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            if (HandlerOfMessageType.ContainsKey(type))
                throw new ArgumentException("Данный тип уже имеет свой обработчик");

            if (string.IsNullOrWhiteSpace(type))
                throw new ArgumentException("Тип сообщения не может быть пустым.");

            HandlerOfMessageType[type] = handler;
            return this;
        }

        public void Clear() => HandlerOfMessageType.Clear();

        /// <summary>
        /// Установить обработчик для <see cref="Message"/>, у которого тип не задан.
        /// </summary>
        public void SetDefaultHandler(Func<TopClient, Message, ServiceRegistry, Task<Message?>> handler)
            => DefaultHandler = handler;

        protected virtual async Task<Message?> DefaultHandlerRealization(TopClient client, Message msg, ServiceRegistry context) => null;
    }

}
