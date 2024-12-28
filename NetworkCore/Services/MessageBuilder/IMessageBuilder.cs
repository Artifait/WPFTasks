using TopNetwork.Core;

namespace TopNetwork.Services.MessageBuilder
{
    public interface IMsgSourceData
    {
        string MessageType { get; }
    }

    public interface IMessageBuilder<TMsgSourceData>
        where TMsgSourceData : IMsgSourceData
    {
        Message BuildMsg();
    }
}
