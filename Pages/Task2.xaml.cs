using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WPFTasks.ViewModels;

namespace WPFTasks.Pages
{
    /// <summary>
    /// Логика взаимодействия для Task2.xaml
    /// </summary>
    public partial class Task2 : Page
    {
        private Mutex _mutex = new();

        public Task2()
        {
            InitializeComponent();
        }
        private async void StartThreads(object sender, RoutedEventArgs e)
        {
            FirstThreadOutput.Clear();
            SecondThreadOutput.Clear();

            var firstThreadTask = Task.Run(DisplayAscending);
            var secondThreadTask = Task.Run(DisplayDescending);

            await Task.WhenAll(firstThreadTask, secondThreadTask);

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
