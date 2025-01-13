
using System.Net.Http;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using WPFTasks.Core.ViewModels.HttpsPartOne;


namespace WPFTasks.Pages
{
    /// <summary>
    /// Логика взаимодействия для Task1.xaml
    /// </summary>
    public partial class Task1 : Page
    {
        private readonly string _hamletUrl = "https://www.gutenberg.org/files/1524/1524-0.txt";
        private string _hashResponse = string.Empty;
        private SemaphoreSlim _semaphore = new(1, 1);
        private DispatcherTimer _timer;
        private DateTime _startTime;

        public Task1()
        {
            InitializeComponent();
        }

        private async Task LoadHamletAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                if (_hashResponse == string.Empty)
                {
                    using var httpClient = new HttpClient();

                    Dispatcher.Invoke(() =>
                    {
                        SetText(HamletText, "Загрузка текста...");
                        StartTimer(); // Запуск таймера
                    });

                    _hashResponse = await httpClient.GetStringAsync(_hamletUrl);
                }

                Dispatcher.Invoke(() =>
                {
                    StopTimer(); // Остановка таймера
                    SetText(HamletText, _hashResponse);
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    StopTimer(); // Остановка таймера
                    MessageBox.Show(ex.Message);
                });
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private void StartTimer()
        {
            _startTime = DateTime.Now;
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100) // Обновление каждые 100 мс
            };
            _timer.Tick += UpdateTimer;
            _timer.Start();
        }

        private void StopTimer()
        {
            if (_timer != null)
            {
                _timer.Stop();
                _timer.Tick -= UpdateTimer;
                _timer = null;
                LoadingView.Text = "Гамлет"; // Очистка таймера
            }
        }

        private void UpdateTimer(object sender, EventArgs e)
        {
            var elapsedTime = DateTime.Now - _startTime;
            LoadingView.Text = $"Время загрузки: {elapsedTime.Seconds}s {elapsedTime.Milliseconds}ms";
        }

        public void SetText(TextBox textBox, string text)
        {
            textBox.Text = text;
        }

        private void OnClick(object sender, RoutedEventArgs e)
        {
            Task.Run(() => LoadHamletAsync());
        }
    }
}
