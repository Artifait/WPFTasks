
using System.Net.Sockets;
using System.Net;

namespace TopNetwork.Core.Defaults
{
    public abstract class DefaultServer
    {
        protected CancellationTokenSource? _cancellationTokenSource = null;
        public IPAddress Address { get; private set; } = null!;
        public int Port { get; private set; }
        public IServerStatus Status { get; set; } = null!;
        public TcpListener Listener { get; set; } = null!;
        public ServerHandlerBase ServerHandlers { get; set; } = null!;

        public virtual void Init(IPAddress address, int port)
        {
            Address = address;
            Port = port;
            Listener = new(Address, Port);
            Status = new DefaultServerStatus();
            ServerHandlers = new();
        }

        public virtual async Task Start()
        {
            if (Status.IsRunning)
                throw new InvalidOperationException("Сервер уже запущен.");
            if(Listener == null)
                throw new NullReferenceException("Инициализируйте");

            _cancellationTokenSource = new CancellationTokenSource();

            Status.StartTime = DateTime.Now;
            Status.IsRunning = true;

            Listener.Start();
            await OnStartAsync(_cancellationTokenSource.Token);
        }

        public async Task Stop()
        {
            if (!Status.IsRunning)
                throw new InvalidOperationException("Сервер не запущен.");

            _cancellationTokenSource!.Cancel();
            _cancellationTokenSource!.Dispose();
            _cancellationTokenSource = null!;

            Status.IsRunning = false;
            Status.StartTime = null;
            Listener.Stop();

            await OnStopAsync();
        }

        public async Task Restart()
        {
            await Stop();
            await Status.SetDefaultState();
            await Start();
        }

        public abstract Task OnStartAsync(CancellationToken cancellationToken);
        public abstract Task OnStopAsync();
    }
}
