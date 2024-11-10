using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WPFTasks.Pages
{
    public partial class Task2 : Page
    {
        private Mutex _mutex = new(false);

        public Task2()
        {
            InitializeComponent();
        }

        private async void StartThreads(object sender, RoutedEventArgs e)
        {
            FirstThreadOutput.Clear();
            SecondThreadOutput.Clear();

            var firstThreadTask = Task.Run(DisplayAscending);
            await firstThreadTask;

            var secondThreadTask = Task.Run(DisplayDescending);
            await secondThreadTask;

            MessageBox.Show("Все потоки завершены.");
        }

        private void DisplayAscending()
        {
            _mutex.WaitOne();
            try
            {
                for (int i = 0; i <= 20; i++)
                {
                    AppendText(FirstThreadOutput, $"Первый поток: {i}");
                    Thread.Sleep(100);
                }
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
        }

        private void DisplayDescending()
        {
            _mutex.WaitOne();
            try
            {
                for (int i = 10; i >= 0; i--)
                {
                    AppendText(SecondThreadOutput, $"Второй поток: {i}");
                    Thread.Sleep(100);
                }
            }
            finally
            {
                _mutex.ReleaseMutex();
            }
        }

        private void AppendText(TextBox textBox, string text)
        {
            Dispatcher.Invoke(() =>
            {
                textBox.AppendText(text + Environment.NewLine);
            });
        }
    }
}
