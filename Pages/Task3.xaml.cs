using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;

namespace WPFTasks.Pages
{
    /// <summary>
    /// Логика взаимодействия для Task3.xaml
    /// </summary>
    public partial class Task3 : Page
    {
        List<int> numbers = null!;

        public Task3()
        {
            InitializeComponent();
        }

        private void StartThreads(object sender, RoutedEventArgs e)
        {
            OutputTextBox.Clear();
            GenerateRandomNumbers();
            Task[] tasks = [
                new Task(FindMax),
                new Task(FindMin),
                new Task(FindAvg),
                new Task(FindSum)
            ];
            
            foreach (var task in tasks) 
                task.Start();
        }

        private void GenerateRandomNumbers()
        {
            var random = new Random();
            numbers = Enumerable.Range(0, 100).Select(_ => random.Next(1, 1000)).ToList();
        }

        private void FindMin()
        {
            AppendText(OutputTextBox, $"Min: {numbers.Min()}");
        }
        private void FindMax()
        {
            AppendText(OutputTextBox, $"Max: {numbers.Max()}");
        }
        private void FindAvg()
        {
            AppendText(OutputTextBox, $"Avg: {numbers.Average()}");
        }
        private void FindSum()
        {
            AppendText(OutputTextBox, $"Avg: {numbers.Sum()}");
        }
        private bool IsPrime(int number)
        {
            if (number <= 1) return false;
            if (number <= 3) return true;
            if (number % 2 == 0 || number % 3 == 0) return false;
            for (int i = 5; i * i <= number; i += 6)
            {
                if (number % i == 0 || number % (i + 2) == 0)
                    return false;
            }
            return true;
        }
        private void ClearTextBox(TextBox textBox) => Dispatcher.Invoke(textBox.Clear);
        private void AppendText(TextBox textBox, string text) => Dispatcher.Invoke(() => textBox.AppendText(text + Environment.NewLine));

    }
}
