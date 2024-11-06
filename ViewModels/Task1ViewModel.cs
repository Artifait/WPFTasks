
using System.ComponentModel;
using System.Text;

namespace WPFTasks.ViewModels
{
    public class Task1ViewModel : INotifyPropertyChanged
    {
        private bool _isPrimeGeneratorEnabled;
        private bool _isFibonacciGeneratorEnabled;
        private string _primeNumbersOutput;
        private string _fibonacciNumbersOutput;
        private CancellationTokenSource _cancellationTokenSource;

        public string PrimeNumbersOutput
        {
            get => _primeNumbersOutput;
            set
            {
                _primeNumbersOutput = value;
                OnPropertyChanged(nameof(PrimeNumbersOutput));
            }
        }

        public string FibonacciNumbersOutput
        {
            get => _fibonacciNumbersOutput;
            set
            {
                _fibonacciNumbersOutput = value;
                OnPropertyChanged(nameof(FibonacciNumbersOutput));
            }
        }

        public bool IsPrimeGeneratorEnabled
        {
            get => _isPrimeGeneratorEnabled;
            set
            {
                _isPrimeGeneratorEnabled = value;
                OnPropertyChanged(nameof(IsPrimeGeneratorEnabled));
                if (value) StartPrimeGenerator();
            }
        }

        public bool IsFibonacciGeneratorEnabled
        {
            get => _isFibonacciGeneratorEnabled;
            set
            {
                _isFibonacciGeneratorEnabled = value;
                OnPropertyChanged(nameof(IsFibonacciGeneratorEnabled));
                if (value) StartFibonacciGenerator();
            }
        }

        public Task1ViewModel()
        {
            _cancellationTokenSource = new CancellationTokenSource();
        }

        private async void StartPrimeGenerator()
        {
            await Task.Run(() =>
            {
                int num = 2;
                var sb = new StringBuilder();
                while (!_cancellationTokenSource.Token.IsCancellationRequested && IsPrimeGeneratorEnabled)
                {
                    if (IsPrime(num))
                    {
                        sb.AppendLine(num.ToString());
                        PrimeNumbersOutput = sb.ToString();
                        Thread.Sleep(500);
                    }
                    num++;
                }
            });
        }

        private async void StartFibonacciGenerator()
        {
            await Task.Run(() =>
            {
                int a = 0, b = 1;
                var sb = new StringBuilder();
                while (!_cancellationTokenSource.Token.IsCancellationRequested && IsFibonacciGeneratorEnabled)
                {
                    int temp = a;
                    a = b;
                    b = temp + b;

                    sb.AppendLine(a.ToString());
                    FibonacciNumbersOutput = sb.ToString();
                    Thread.Sleep(500);
                }
            });
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

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
