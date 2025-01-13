
using System.Diagnostics;
using System.Timers;
using System.Windows;
using System.Windows.Controls;
using WPFTasks.Core.ViewModels.HttpsPartOne;

namespace WPFTasks.Pages
{
    /// <summary>
    /// Логика взаимодействия для Task5.xaml
    /// </summary>
    public partial class Task5 : Page
    {
        private readonly BookDownloader _bookDownloader = new();
        private const string DownloadDirectory = "Books";

        public Task5()
        {
            InitializeComponent();
        }

        private void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            string author = AuthorTextBox.Text;
            StatusTextBlock.Text = "Статус: Инициализация...";
            _bookDownloader.StartDownload(author, DownloadDirectory, status => StatusTextBlock.Text = status);
        }
    }

    public class BookDownloader
    {
        private readonly GutenbergApiClient _apiClient = new();
        private readonly Stopwatch _stopwatch = new();
        private System.Timers.Timer _timer;
        private Action<string> _updateStatusCallback;

        public void StartDownload(string author, string downloadDirectory, Action<string> updateStatusCallback)
        {
            if (string.IsNullOrWhiteSpace(author))
            {
                MessageBox.Show("Укажите имя и фамилию автора.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _updateStatusCallback = updateStatusCallback;
            _stopwatch.Reset();
            _stopwatch.Start();

            _timer = new System.Timers.Timer(100);
            _timer.Elapsed += TimerElapsed;
            _timer.Start();

            Task.Run(async () =>
            {
                try
                {
                    await _apiClient.DownloadBooksByAuthorAsync(author, downloadDirectory);
                    _stopwatch.Stop();
                    _timer.Stop();
                    _ = MessageBox.Show("Загрузка завершена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    _stopwatch.Stop();
                    _timer.Stop();
                    _ = MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    _stopwatch.Stop();
                    _timer.Stop();
                    _updateStatusCallback?.Invoke($"Статус: Загрузка завершена за {_stopwatch.Elapsed:g}");
                }
            });
        }

        private void TimerElapsed(object sender, ElapsedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                _updateStatusCallback?.Invoke($"Статус: Загрузка... Прошло {_stopwatch.Elapsed:g}");
            });
        }
    }
}
