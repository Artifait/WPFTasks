using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace WPFTasks.Pages
{
    public partial class Task1 : Page
    {
        private CancellationTokenSource _numberTokenSource;
        private CancellationTokenSource _letterTokenSource;
        private CancellationTokenSource _symbolTokenSource;
        private readonly Random _random = new Random();
        private readonly int _delay = 500; // Задержка между итерациями в миллисекундах

        public Task1()
        {
            InitializeComponent();

            CheckNumber.Checked += (s, e) => StartNumberGenerator();
            CheckNumber.Unchecked += (s, e) => StopNumberGenerator();

            CheckLetter.Checked += (s, e) => StartLetterGenerator();
            CheckLetter.Unchecked += (s, e) => StopLetterGenerator();

            CheckSymbol.Checked += (s, e) => StartSymbolGenerator();
            CheckSymbol.Unchecked += (s, e) => StopSymbolGenerator();
        }

        private async void StartNumberGenerator()
        {
            if (_numberTokenSource != null) return;

            _numberTokenSource = new CancellationTokenSource();
            var token = _numberTokenSource.Token;

            try
            {
                await Task.Run(async () =>
                {
                    while (!token.IsCancellationRequested)
                    {
                        await Dispatcher.InvokeAsync(() => AddCharacter('N'));
                        await Task.Delay(_delay, token);
                    }
                }, token);
            }
            catch (TaskCanceledException) {  }
        }

        private void StopNumberGenerator()
        {
            _numberTokenSource?.Cancel();
            _numberTokenSource = null;
        }

        private async void StartLetterGenerator()
        {
            if (_letterTokenSource != null) return;

            _letterTokenSource = new CancellationTokenSource();
            var token = _letterTokenSource.Token;

            try
            {
                await Task.Run(async () =>
                {
                    while (!token.IsCancellationRequested)
                    {
                        await Dispatcher.InvokeAsync(() => AddCharacter('L'));
                        await Task.Delay(_delay, token);
                    }
                }, token);
            }
            catch (TaskCanceledException) { }
        }

        private void StopLetterGenerator()
        {
            _letterTokenSource?.Cancel();
            _letterTokenSource = null;
        }

        private async void StartSymbolGenerator()
        {
            if (_symbolTokenSource != null) return;

            _symbolTokenSource = new CancellationTokenSource();
            var token = _symbolTokenSource.Token;

            try
            {
                await Task.Run(async () =>
                {
                    while (!token.IsCancellationRequested)
                    {
                        await Dispatcher.InvokeAsync(() => AddCharacter('S'));
                        await Task.Delay(_delay, token);
                    }
                }, token);
            }
            catch (TaskCanceledException) { }
        }

        private void StopSymbolGenerator()
        {
            _symbolTokenSource?.Cancel();
            _symbolTokenSource = null;
        }

        private void AddCharacter(char type)
        {
            char character = type switch
            {
                'N' => (char)('0' + _random.Next(10)), 
                'L' => (char)('A' + _random.Next(26)),
                'S' => new[] { '!', '@', '#', '$', '%', '^', '&', '*', '(', ')' }[_random.Next(10)],
                _ => throw new ArgumentException("Invalid character type")
            };

            Output.Text += character;
        }
    }
}
