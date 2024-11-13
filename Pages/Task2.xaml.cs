using System;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WPFTasks.Pages
{
    public partial class Task2 : Page
    {
        public Task2()
        {
            InitializeComponent();
        }

        private async void CalculatePrimeNumbers(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(StartRangeTextBox.Text, out int startRange))
            {
                startRange = 2; 
            }
            if (!int.TryParse(EndRangeTextBox.Text, out int endRange) || endRange < startRange)
            {
                endRange = startRange + 1000; 
            }

            ClearTextBox(OutputTextBox);
            AppendText(OutputTextBox, "Идет расчет...");

            try
            {
                int primeCount = 0;
                Task<string> task = Task.Run(() => CalcPrimesInRange(startRange, endRange, out primeCount));

                string result = await task;

                ClearTextBox(OutputTextBox);
                AppendText(OutputTextBox, result);
                AppendText(OutputTextBox, $"Количество простых чисел: {primeCount}");
            }
            catch (Exception ex)
            {
                AppendText(OutputTextBox, $"Ошибка: {ex.Message}");
            }
        }

        private string CalcPrimesInRange(int start, int end, out int primeCount)
        {
            StringBuilder sb = new();
            primeCount = 0;

            for (int num = start; num <= end; num++)
            {
                if (IsPrime(num))
                {
                    sb.AppendLine(num.ToString());
                    primeCount++;
                }
            }

            return sb.ToString();
        }

        private static bool IsPrime(int number)
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
