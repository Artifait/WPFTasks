
using TopNetwork.Core;

namespace TopNetwork.Services.MessageBuilder
{
    public class AuthenticationRequestData : IMsgSourceData
    {
        public string Login { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public string MessageType => "AuthenticationRequest";
    }

    public class AuthenticationRequestMessageBuilder : IMessageBuilder<AuthenticationRequestData>
    {
        private AuthenticationRequestData _data = new();
        public AuthenticationRequestMessageBuilder SetLogin(string login)
        {
            _data.Login = login;
            return this;
        }

        public AuthenticationRequestMessageBuilder SetPassword(string password)
        {
            _data.Password = password;
            return this;
        }
        public Message BuildMsg()
        {
            return new Message
            {
                MessageType = _data.MessageType,
                Payload = $"{_data.Login}:{_data.Password}",
            };
        }

        public AuthenticationRequestData Parse(Message msg)
        {
            if (msg.MessageType != _data.MessageType)
                throw new InvalidOperationException("Incorrect message type.");

            var parts = msg.Payload.Split(':');
            return new AuthenticationRequestData
            {
                Login = parts[0],
                Password = parts[1]
            };
        }
    }
}
