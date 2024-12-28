using TopNetwork.Core;

namespace TopNetwork.Services.MessageBuilder
{
    public class AuthenticationResponseData : IMsgSourceData
    {
        public string Payload { get; set; } = string.Empty;
        public bool IsAuthenticated { get; set; } = false;

        public string MessageType => MsgType;
        public static string MsgType => "AuthenticationResponse";
    }

    public class AuthenticationResponseMessageBuilder : IMessageBuilder<AuthenticationResponseData>
    {
        private AuthenticationResponseData _data = new();

        public AuthenticationResponseMessageBuilder SetAuthentication(bool authentication)
        {
            _data.IsAuthenticated = authentication;
            return this;
        }
        public AuthenticationResponseMessageBuilder SetExplanatoryMsg(string payload)
        {
            _data.Payload = payload;
            return this;
        }

        public Message BuildMsg()
        {
            return new()
            {
                MessageType = _data.MessageType,
                Headers = { { "IsAuthed", _data.IsAuthenticated!.ToString()! } },
                Payload = _data.Payload
            };
        }

        public static AuthenticationResponseData Parse(Message msg)
        {
            if (msg.MessageType != AuthenticationResponseData.MsgType)
                throw new InvalidOperationException("Incorrect message type.");

            if (msg.Headers.TryGetValue("IsAuthed", out var value))
                return new()
                {
                    IsAuthenticated = bool.Parse(value),
                    Payload = msg.Payload
                };

            throw new InvalidDataException("Нету заголовка IsAuthed");
        }
    }
}
