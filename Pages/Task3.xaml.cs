using System;
using System.Collections.Generic;
using System.IO;
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

namespace WPFTasks.Pages
{
    /// <summary>
    /// Логика взаимодействия для Task3.xaml
    /// </summary>
    public partial class Task3 : Page
    {
        private static readonly Mutex _mutex1 = new Mutex();
        private static readonly Mutex _mutex2 = new Mutex();
        private static readonly Mutex _mutex3 = new Mutex();

        private const string File1 = "RandomNumbers.txt";
        private const string File2 = "PrimeNumbers.txt";
        private const string File3 = "PrimeEndingWith7.txt";

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
        }

        private void GenerateRandomNumbers()
        {
            //_mutex1.WaitOne(); 

            try
            {
                var random = new Random();
                List<int> numbers = Enumerable.Range(0, 100).Select(_ => random.Next(1, 1000)).ToList();

                File.WriteAllLines(File1, numbers.Select(n => n.ToString()));
                Dispatcher.Invoke(() => OutputTextBox.AppendText($"{DateTime.Now.ToLongDateString()} - Первый поток: Сгенерированы случайные числа и записаны в файл.\n"));
            }
            finally
            {
                _mutex2.ReleaseMutex(); // Разблокируем мьютекс для второго потока
            }
        }

        private void FilterPrimes()
        {
            _mutex2.WaitOne(); // Ждем разблокировки мьютекса от первого потока

            try
            {
                var numbers = File.ReadAllLines(File1).Select(int.Parse);
                var primes = numbers.Where(IsPrime).ToList();

                File.WriteAllLines(File2, primes.Select(n => n.ToString()));
                Dispatcher.Invoke(() => OutputTextBox.AppendText("Второй поток: Простые числа записаны во второй файл.\n"));
            }
            finally
            {
                _mutex3.ReleaseMutex(); // Разблокируем мьютекс для третьего потока
            }
        }

        private void FilterPrimesEndingWith7()
        {
            _mutex3.WaitOne(); // Ждем разблокировки мьютекса от второго потока

            try
            {
                var primes = File.ReadAllLines(File2).Select(int.Parse);
                var primesEndingWith7 = primes.Where(n => n % 10 == 7).ToList();

                File.WriteAllLines(File3, primesEndingWith7.Select(n => n.ToString()));
                Dispatcher.Invoke(() => OutputTextBox.AppendText("Третий поток: Простые числа, оканчивающиеся на 7, записаны в третий файл.\n"));
            }
            finally
            {
                //_mutex1.ReleaseMutex();
            }
        }
    }
}

