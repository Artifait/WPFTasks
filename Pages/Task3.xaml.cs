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
        private static readonly AutoResetEvent _event1 = new(true);  
        private static readonly AutoResetEvent _event2 = new(false); 
        private static readonly AutoResetEvent _event3 = new(false); 
        private static readonly AutoResetEvent _event4 = new(false); 

        private const string File1 = "RandomNumbers.txt";
        private const string File2 = "PrimeNumbers.txt";
        private const string File3 = "PrimeEndingWith7.txt";
        private const string ReportFile = "Report.txt";

        public Task3()
        {
            InitializeComponent();
        }

        private void StartThreads(object sender, RoutedEventArgs e)
        {
            OutputTextBox.Clear();

            new Thread(GenerateRandomNumbers).Start();
            new Thread(FilterPrimes).Start();
            new Thread(FilterPrimesEndingWith7).Start();
            new Thread(GenerateReport).Start();
        }

        private void GenerateRandomNumbers()
        {
            _event1.WaitOne(); 

            try
            {
                var random = new Random();
                List<int> numbers = Enumerable.Range(0, 100).Select(_ => random.Next(1, 1000)).ToList();

                File.WriteAllLines(File1, numbers.Select(n => n.ToString()));
                Dispatcher.Invoke(() => OutputTextBox.AppendText($"{DateTime.Now.ToString("HH:mm:ss-FFFF")} - Первый поток: Сгенерированы случайные числа и записаны в файл.\n"));
            }
            finally
            {
                _event2.Set(); 
            }
        }

        private void FilterPrimes()
        {
            _event2.WaitOne(); 

            try
            {
                var numbers = File.ReadAllLines(File1).Select(int.Parse);
                var primes = numbers.Where(IsPrime).ToList();

                File.WriteAllLines(File2, primes.Select(n => n.ToString()));
                Dispatcher.Invoke(() => OutputTextBox.AppendText($"{DateTime.Now.ToString("HH:mm:ss-FFFF")} - Второй поток: Простые числа записаны во второй файл.\n"));
            }
            finally
            {
                _event3.Set(); 
            }
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

        private void FilterPrimesEndingWith7()
        {
            _event3.WaitOne();

            try
            {
                var primes = File.ReadAllLines(File2).Select(int.Parse);
                var primesEndingWith7 = primes.Where(n => n % 10 == 7).ToList();

                File.WriteAllLines(File3, primesEndingWith7.Select(n => n.ToString()));
                Dispatcher.Invoke(() => OutputTextBox.AppendText($"{DateTime.Now.ToString("HH:mm:ss-FFFF")} - Третий поток: Простые числа, оканчивающиеся на 7, записаны в третий файл.\n"));
            }
            finally
            {
                _event4.Set();
            }
        }

        private void GenerateReport()
        {
            _event4.WaitOne(); 

            try
            {
                StringBuilder report = new StringBuilder();
                report.AppendLine("Отчёт о полученных файлах:");

                GenerateFileReport(File1, "Файл случайных чисел", report);
                GenerateFileReport(File2, "Файл простых чисел", report);
                GenerateFileReport(File3, "Файл простых чисел, оканчивающихся на 7", report);

                File.WriteAllText(ReportFile, report.ToString());
                Dispatcher.Invoke(() => OutputTextBox.AppendText($"{DateTime.Now.ToString("HH:mm:ss-FFFF")} - Четвертый поток: Отчет создан и записан в файл.\n"));
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => OutputTextBox.AppendText($"Ошибка при создании отчета: {ex.Message}\n"));
            }
        }

        private void GenerateFileReport(string filePath, string description, StringBuilder report)
        {
            if (!File.Exists(filePath))
            {
                report.AppendLine($"{description}: файл не найден.");
                return;
            }

            var lines = File.ReadAllLines(filePath);
            var fileSize = new FileInfo(filePath).Length;

            report.AppendLine($"\n{description}:");
            report.AppendLine($"- Количество чисел: {lines.Length}");
            report.AppendLine($"- Размер файла: {fileSize} байт");
            report.AppendLine($"- Содержимое файла:\n  {string.Join(", ", lines)}");
        }
    }
}
