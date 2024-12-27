
using System.Net;
using System.Net.Sockets;
using TopNetwork.Core;
using TopNetwork.RequestResponse;

namespace WPFTasks.Core.Models.Currency
{
    public class CurrencyClient
    {
        private static RrClientHandlerBase _Handlers = new RrClientHandlerBase()
            .AddHandlerForMessageType("";

        private RrClient _client;
        private bool _isAuth;

        public bool IsConnected => _client?.IsActive ?? false;
        public bool IsAuth
        {
            get => _isAuth;
            set
            {
                _isAuth = value;

                if(value)
                    OnStartAuthSession?.Invoke();
                else
                    OnEndAuthSession?.Invoke();
            }
        }

        public event Action? OnStartAuthSession;
        public event Action? OnEndAuthSession;


        public void Init(string IpServer, int port)
        {
            _client = new RrClient
            (
                new TopClient().Connect
                (
                    new TcpClient
                    (
                        new IPEndPoint
                        (
                            IPAddress.Parse(IpServer),
                            port
                        )
                    )
                )
            );
        }

        public 
    }
}