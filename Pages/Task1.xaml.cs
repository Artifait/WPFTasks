
using System.Windows;
using System.Windows.Controls;
using WPFTasks.ViewModels;

namespace WPFTasks.Pages
{
    public partial class Task1 : Page
    {
        private SemaphoreSlim _semaphore = new(3);

        public Task1()
        {
            InitializeComponent();
        }
        private async void StartThreads(object sender, RoutedEventArgs e)
        {
            OutputTextBox.Clear(); // Очистка поля вывода
            var tasks = Enumerable.Range(1, 10).Select(i => Task.Run(() => RunThread(i)));
            await Task.WhenAll(tasks); // Ожидаем завершения всех потоков
        }

        private async Task RunThread(int threadId)
        {
            await _semaphore.WaitAsync(); // Ожидание доступного слота

            try
            {
                var random = new Random();
                var numbers = Enumerable.Range(0, 5).Select(_ => random.Next(100)).ToArray();
                var message = $"Поток {threadId} (ID: {Thread.CurrentThread.ManagedThreadId}): " +
                              $"{string.Join(", ", numbers)}";
                AppendText(message);
                await Task.Delay(750); 
            }
            finally
            {
                _semaphore.Release();
            }
        }

        private void AppendText(string text)
        {
            Dispatcher.Invoke(() =>
            {
                OutputTextBox.AppendText(text + Environment.NewLine);
            });
        }
    }
}
