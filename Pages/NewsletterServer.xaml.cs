
using Microsoft.Toolkit.Uwp.Notifications;
using System.Windows;
using System.Windows.Controls;
using TopNetwork.Core;
using WPFTasks.Core.Models.ModuleFour;

namespace WPFTasks.Pages
{
    /// <summary>
    /// Логика взаимодействия для NewsletterServer.xaml
    /// </summary>
    public partial class NewsletterServer : Page
    {
        private readonly List<(TextBox KeyTextBox, TextBox ValueTextBox)> _headers = new();
        private static UdpServer? _server;

        public NewsletterServer()
        {
            InitializeComponent();

            if(_server == null )
            {
                _server = new(12021);

                _server.OnClientSubscribe += (client, type) => new ToastContentBuilder()
                    .SetToastDuration(ToastDuration.Short)
                    .AddText("Клиент подписался")
                    .AddText($"Рассылка.{type} += [{client}]")
                    .Show();

                _server.OnClientUnsubscribe += (client, type) => new ToastContentBuilder()
                    .SetToastDuration(ToastDuration.Short)
                    .AddText("Клиент отписался")
                    .AddText($"Рассылка.{type} -= [{client}]")
                    .Show();

                _server.OnErrore += msg => new ToastContentBuilder()
                    .SetToastDuration(ToastDuration.Short)
                    .AddText("Ошибка Сервера")
                    .AddText(msg)
                    .Show();

                _ = _server.StartAsync();
            }
        }

        private void AddHeaderButton_Click(object sender, RoutedEventArgs e)
        {
            var headerRow = new Grid
            {
                Margin = new Thickness(0, 5, 0, 5)
            };

            // Определяем колонки: первая (ключ) занимает 1*, вторая (значение) - 2*, третья (кнопка) фиксирована
            headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
            headerRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });

            var keyTextBox = new TextBox
            {
                Style = (Style)FindResource("TextBoxStyle"),
                Margin = new Thickness(0, 0, 5, 0),
                ToolTip = "Header Key",
                FontSize = 18
            };
            Grid.SetColumn(keyTextBox, 0);

            var valueTextBox = new TextBox
            {
                Style = (Style)FindResource("TextBoxStyle"),
                Margin = new Thickness(0, 0, 5, 0),
                ToolTip = "Header Value",
                FontSize = 18
            };
            Grid.SetColumn(valueTextBox, 1);

            var deleteButton = new Button
            {
                Content = "❎",
                Style = (Style)FindResource("DarkButtonStyle"),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(deleteButton, 2);

            deleteButton.Click += (s, args) =>
            {
                HeadersPanel.Children.Remove(headerRow);
                _headers.Remove((keyTextBox, valueTextBox));
            };

            headerRow.Children.Add(keyTextBox);
            headerRow.Children.Add(valueTextBox);
            headerRow.Children.Add(deleteButton);

            HeadersPanel.Children.Add(headerRow);
            _headers.Add((keyTextBox, valueTextBox));
        }

        private void ClearFormButton_Click(object sender, RoutedEventArgs e)
        {
            MessageTypeTextBox.Clear();
            PayloadTextBox.Clear();
            HeadersPanel.Children.Clear();
            _headers.Clear();
        }

        private void SendMessageButton_Click(object sender, RoutedEventArgs e)
        {
            // Создание сообщения из данных формы
            var message = new Message
            {
                MessageType = MessageTypeTextBox.Text,
                Payload = PayloadTextBox.Text,
            };
            
            foreach (var (keyTextBox, valueTextBox) in _headers)
            {
                if (!string.IsNullOrWhiteSpace(keyTextBox.Text) && !string.IsNullOrWhiteSpace(valueTextBox.Text))
                {
                    message.Headers[keyTextBox.Text] = valueTextBox.Text;
                }
            }

            Task.Run(() => _server?.SendMessageAsync(message));
        }
    }
}
