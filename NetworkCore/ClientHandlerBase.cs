
using System.Collections.Concurrent;

namespace TopNetwork.Core
{
    public delegate void HandlerMsgFromServer(Message message);

    public class ClientHandlerBase
    {
        public ConcurrentDictionary<string, HandlerMsgFromServer> HandlerOfMessageType { get; private set; }
        public HandlerMsgFromServer DefaultHandler { get; private set; }

        public ClientHandlerBase()
        {
            DefaultHandler = DefaultHandlerRealization;
            HandlerOfMessageType = [];
        }

        public void HandleMessage(Message msg)
        {
            string msgType = msg.MessageType;

            if (HandlerOfMessageType.TryGetValue(msgType, out var handler))
            {
                handler(msg);
                return;
            }

            DefaultHandler(msg);
        }

        public void AddHandlerForMessageType(string type, HandlerMsgFromServer handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            if (HandlerOfMessageType.ContainsKey(type))
                throw new ArgumentException("Данный тип сообщения уже иммет свой обрабтчик");

            if (string.IsNullOrWhiteSpace(type))
                throw new ArgumentException("Тип сообщения не может быть пустым.");

            HandlerOfMessageType[type] = handler;
        }

        /// <summary>
        /// Установить обработчик для <see cref="Message"/> у которого тип не задан
        /// </summary>
        public void SetDefaultHandler(HandlerMsgFromServer handler) => DefaultHandler = handler;
        protected virtual void DefaultHandlerRealization(Message msg) { }
    }
}
