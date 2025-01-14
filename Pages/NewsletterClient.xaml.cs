
using System.Collections.ObjectModel;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using TopNetwork.Core;
using WPFTasks.Core.Models.ModuleFour;

namespace WPFTasks.Pages
{
    public class ViewMessage
    {
        public string TimeReceived { get; set; } = DateTime.Now.ToString("f");
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Логика взаимодействия для NewsletterClient.xaml
    /// </summary>
    public partial class NewsletterClient : Page
    {
        private readonly UdpClientWrapper _client;

        public ObservableCollection<ViewMessage> Messages { get; set; } = new();

        public NewsletterClient()
        {
            InitializeComponent();

            _client = new UdpClientWrapper(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 12021), 0);

            _client.OnMessageReceived += HandleMessageReceived;
            _client.OnErrore += HandleError;

            MessagesList.ItemsSource = Messages;

            if (!_client.IsStarted)
                _ = _client.StartListeningAsync();
        }

        private async void SubscribeButton_Click(object sender, RoutedEventArgs e)
        {
            string messageType = SubscriptionTypeTextBox.Text;

            if (string.IsNullOrWhiteSpace(messageType))
            {
                MessageBox.Show("Введите тип подписки.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            await _client.SubscribeToMessageTypeAsync(messageType);
        }

        private async void UnsubscribeButton_Click(object sender, RoutedEventArgs e)
        {
            string messageType = SubscriptionTypeTextBox.Text;

            if (string.IsNullOrWhiteSpace(messageType))
            {
                MessageBox.Show("Введите тип подписки для отписки.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var unsubscribeMessage = new Message
            {
                MessageType = "Unsubscribe",
                Headers = new Dictionary<string, string>
            {
                { "MessageType", messageType }
            }
            };

            await _client.SendMessageAsync(unsubscribeMessage);
        }

        private void HandleMessageReceived(Message message)
        {
            Dispatcher.Invoke(() =>
            {
                Messages.Add(new() { Message = message.ToString() });

                LastMessageTextBox.Text = message.ToString();
            });
        }

        private void HandleError(string errorMessage)
        {
            MessageBox.Show(errorMessage, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            _client.StopListening();
        }
    }
}
