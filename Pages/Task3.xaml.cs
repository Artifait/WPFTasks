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

namespace WPFTasks.Pages
{
    /// <summary>
    /// Логика взаимодействия для Task3.xaml
    /// </summary>
    public partial class Task3 : Page
    {
        public Task3()
        {
            InitializeComponent();
        }

        private async void CalculateFibonacciAsync_Click(object sender, RoutedEventArgs e)
        {
            ResultTextBox.Text = "Вычисление...";
            int boundary;

            if (int.TryParse(BoundaryTextBox.Text, out boundary) && boundary >= 0)
            {
                var result = await Task.Run(() => CalculateFibonacci(boundary));
                ResultTextBox.Text = string.Join(", ", result);
            }
            else
            {
                ResultTextBox.Text = "Введите корректное положительное число.";
            }
        }

        private List<int> CalculateFibonacci(int max)
        {
            List<int> fibonacciNumbers = new List<int> { 0, 1 };
            int next = 1;

            while (next <= max)
            {
                fibonacciNumbers.Add(next);
                int count = fibonacciNumbers.Count;
                next = fibonacciNumbers[count - 1] + fibonacciNumbers[count - 2];
            }

            return fibonacciNumbers;
        }
    }
}

