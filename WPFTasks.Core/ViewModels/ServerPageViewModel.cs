
using System.Net;
using System.Windows;
using System.Windows.Input;
using WPFTasks.Core.Models.Currency;
using WPFTasks.ViewModels;

namespace WPFTasks.Core.ViewModels
{
    public class ServerPageViewModel : BaseViewModel
    {
        private CurrencyServer _currencyServer;

        public ServerPageViewModel()
        {
            StartServerCommand = new RelayCommand(StartServer, _ => CanStartServer);
            StopServerCommand = new RelayCommand(StopServer, _ => CanStopServer);
            ClearMessagesCommand = new RelayCommand(ClearMessages);
        }

        private string _ipAddress = "127.0.0.1";
        public string IpAddress
        {
            get => _ipAddress;
            set => SetProperty(ref _ipAddress, value);
        }

        private string _port = "8080";
        public string Port
        {
            get => _port;
            set => SetProperty(ref _port, value);
        }

        private string _content = "";
        public string Content
        {
            get => _content;
            set => SetProperty(ref _content, value);
        }
        public ICommand StartServerCommand { get; }
        public ICommand StopServerCommand { get; }
        public ICommand ClearMessagesCommand { get; }

        private bool _isServerRunning;
        public bool IsServerRunning
        {
            get => _isServerRunning;
            private set
            {
                if (SetProperty(ref _isServerRunning, value))
                {
                    OnPropertyChanged(nameof(CanStartServer));
                    OnPropertyChanged(nameof(CanStopServer));
                }
            }
        }

        public bool CanStartServer => !IsServerRunning;
        public bool CanStopServer => IsServerRunning;

        private void StartServer(object _)
        {
            try
            {
                var ip = IPAddress.Parse(IpAddress);
                var port = int.Parse(Port);

                _currencyServer = new CurrencyServer(ip, port);
                _currencyServer.Logger.OnUpdateLog += OnUpdateLog;
                _currencyServer.Start();
                IsServerRunning = true;

            }
            catch (Exception ex)
            {

            }
        }

        private void StopServer(object _)
        {
            try
            {
                _currencyServer.Server.Stop();
                IsServerRunning = false;
            }
            catch (Exception ex)
            {
            }
        }

        private void ClearMessages(object _)
        {
            Content = String.Empty;
        }
        private void OnUpdateLog(string @new)
        {
            MessageBox.Show("Fds");
            Content = @new; 
        }
    }
}
