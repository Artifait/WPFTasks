using Microsoft.Win32;
using System.Buffers;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;


namespace WPFTasks.Pages
{
    /// <summary>
    /// Логика взаимодействия для Task4.xaml
    /// </summary>
    public partial class Task4 : Page
    {
        List<int> numbers = null!;

        public Task4()
        {
            InitializeComponent();
        }
        private void StartThreads(object sender, RoutedEventArgs e)
        {
            GenerateRandomNumbers();
            OutputTextBox.Clear();

            Task task1 = new(RemoveDuplicate);
            Task task2 = new(Sort);
            Task task3 = new(BinSearch);
            task1.ContinueWith(t => task2.Start());
            task2.ContinueWith(t => task3.Start());
            task1.Start();
        }

        private void RemoveDuplicate()
        {
            var dupls =  numbers.GroupBy(x => x)
                          .Where(g => g.Count() > 1)
                          .Select(g => g.Key)
                          .ToList();

            numbers.Distinct();
            AppendText(OutputTextBox, "Были удалены дубликаты следующих чисел: " + string.Join(", ", dupls));
        }

        private void Sort()
        {
            numbers.Sort();
            AppendText(OutputTextBox, "Массив отсортирован");
        }
        private void BinSearch()
        {
            var rnd = new Random();
            int searchNumber = rnd.Next(1, 1000);

            int index = numbers.BinarySearch(searchNumber);

            AppendText(OutputTextBox, index == -1 ? $"Число: {searchNumber} не найдено" : $"Число: {searchNumber} найдено на {index} индексе");
        }
        private void GenerateRandomNumbers()
        {
            var random = new Random();
            numbers = Enumerable.Range(0, 100).Select(_ => random.Next(1, 1000)).ToList();
        }

        private void AppendText(TextBox textBox, string text) => Dispatcher.Invoke(() => textBox.AppendText(text + Environment.NewLine));

    }
}
