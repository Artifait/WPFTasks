
using System.Collections.Concurrent;
using TopNetwork.Core;

namespace TopNetwork.RequestResponse
{
    public class RrClientHandlerBase
    {
        public ConcurrentDictionary<string, Func<Message, Task<Message?>>> HandlerOfMessageType { get; private set; }

        public RrClientHandlerBase()
        {
            HandlerOfMessageType = [];
        }

        public async Task<Message?> HandleMessage(Message msg)
        {
            string msgType = msg.MessageType;

            if (HandlerOfMessageType.TryGetValue(msgType, out var func))
                return await func(msg);

            return null;
        }

        public RrClientHandlerBase AddHandlerForMessageType(string type, Func<Message, Task<Message?>> handler)
        {
            ArgumentNullException.ThrowIfNull(handler);
            ArgumentException.ThrowIfNullOrWhiteSpace(type);

            if (HandlerOfMessageType.ContainsKey(type))
                throw new ArgumentException("Данный тип уже иммет свой обрабтчик");

            HandlerOfMessageType[type] = handler;
            return this;
        }


        public void Clear() => HandlerOfMessageType.Clear();
    }
}
