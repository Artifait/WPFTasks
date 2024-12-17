
using System.Net;
using System.Windows;
using System.Windows.Input;
using WPFTasks.Core.Models.Currency;
using WPFTasks.ViewModels;

namespace WPFTasks.Core.ViewModels
{
    public class ServerPageViewModel : BaseViewModel
    {
        private static CurrencyServer _currencyServer = null!;

        public ServerPageViewModel()
        {
            StartServerCommand = new RelayCommand(StartServer, _ => true);
            StopServerCommand = new RelayCommand(StopServer, _ => true);
            ClearMessagesCommand = new RelayCommand(ClearMessages);

            if(_currencyServer != null)
            {
                _currencyServer.Logger.OnUpdateLog += OnUpdateLog;
                Content = _currencyServer.Logger.LogMsgs;
            }
        }

        private string _ipAddress = "127.0.0.1";
        public string IpAddress
        {
            get => _ipAddress;
            set => SetProperty(ref _ipAddress, value);
        }

        private string _port = "8280";
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

        private void StartServer(object _)
        {
            try
            {
                if(_currencyServer != null && _currencyServer.Status.IsRunning)
                {
                    MessageBox.Show("Сервер уже запущен...", "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                var ip = IPAddress.Parse(IpAddress);
                var port = int.Parse(Port);

                _currencyServer = new CurrencyServer(ip, port);
                _currencyServer.Logger.OnUpdateLog += OnUpdateLog;
                _currencyServer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка при старте сервера...", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void StopServer(object _)
        {
            try
            {
                if(_currencyServer == null || (_currencyServer != null &&  !_currencyServer.Status.IsRunning))
                {
                    MessageBox.Show("Сервер не запущен...\nИли не инициализирован...");
                    return;
                }
                _currencyServer.Server.Stop();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка при остановки сервера...", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearMessages(object _)
        {
            _currencyServer.Logger.ClearLog();
            Content = string.Empty;
        }
        private void OnUpdateLog(string @new)
        {
            Content = @new; 
        }
    }
}
