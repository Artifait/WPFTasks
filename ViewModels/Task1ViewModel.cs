using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace WPFTasks.ViewModels
{
    public class Task1ViewModel : INotifyPropertyChanged
    {
        private bool _isPrimeGeneratorEnabled;
        private bool _isFibonacciGeneratorEnabled;
        private string _primeNumbersOutput;
        private string _fibonacciNumbersOutput;
        private CancellationTokenSource _primeCancellationTokenSource;
        private CancellationTokenSource _fibonacciCancellationTokenSource;

        public Task1ViewModel()
        {
            ResetPrimeGeneratorCommand = new RelayCommand(ResetPrimeGenerator);
            ResetFibonacciGeneratorCommand = new RelayCommand(ResetFibonacciGenerator);
        }

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
                if (value && _primeCancellationTokenSource == null) StartPrimeGenerator();
            }
        }

        public bool IsFibonacciGeneratorEnabled
        {
            get => _isFibonacciGeneratorEnabled;
            set
            {
                _isFibonacciGeneratorEnabled = value;
                OnPropertyChanged(nameof(IsFibonacciGeneratorEnabled));
                if (value && _fibonacciCancellationTokenSource == null) StartFibonacciGenerator();
            }
        }

        public ICommand ResetPrimeGeneratorCommand { get; }
        public ICommand ResetFibonacciGeneratorCommand { get; }

        private void ResetPrimeGenerator()
        {
            _primeCancellationTokenSource?.Cancel();
            _primeCancellationTokenSource = null;
            PrimeNumbersOutput = string.Empty;
            if (IsPrimeGeneratorEnabled) StartPrimeGenerator();
        }

        private void ResetFibonacciGenerator()
        {
            _fibonacciCancellationTokenSource?.Cancel();
            _fibonacciCancellationTokenSource = null;
            FibonacciNumbersOutput = string.Empty;
            if (IsFibonacciGeneratorEnabled) StartFibonacciGenerator();
        }

        private async void StartPrimeGenerator()
        {
            _primeCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _primeCancellationTokenSource.Token;

            await Task.Run(() =>
            {
                int num = 2;
                try
                {
                    int startValue = int.Parse(File.ReadAllText("../../../StartPrimeValue.txt"));
                    num = startValue;
                }
                catch { }
                var sb = new StringBuilder();

                while (!cancellationToken.IsCancellationRequested)
                {
                    if (!IsPrimeGeneratorEnabled)
                    {
                        Thread.Sleep(500);
                        continue;
                    }

                    if (IsPrime(num))
                    {
                        sb.AppendLine(num.ToString());
                        PrimeNumbersOutput = sb.ToString();
                        Thread.Sleep(500);
                    }
                    num++;
                }
            }, cancellationToken).ConfigureAwait(false);
        }

        private async void StartFibonacciGenerator()
        {
            _fibonacciCancellationTokenSource = new CancellationTokenSource();
            var cancellationToken = _fibonacciCancellationTokenSource.Token;

            await Task.Run(() =>
            {
                int a = 1, b = 1;
                var sb = new StringBuilder();

                while (!cancellationToken.IsCancellationRequested)
                {
                    if (!IsFibonacciGeneratorEnabled)
                    {
                        Thread.Sleep(500);
                        continue;
                    }

                    sb.AppendLine(a.ToString());
                    FibonacciNumbersOutput = sb.ToString();

                    int temp = a;
                    a = b;
                    b = temp + b;
                    Thread.Sleep(500);
                }
            }, cancellationToken).ConfigureAwait(false);
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

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute();
        public void Execute(object parameter) => _execute();
        public event EventHandler CanExecuteChanged;

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
