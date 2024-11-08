using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace WPFTasks.ViewModels
{
    public static class ColorGenerator
    {
        private static readonly Random rnd = new Random();

        public static List<Color> GenerateHarmoniousColors()
        {
            Color primaryColor = Color.FromArgb(
                255,
                (byte)rnd.Next(256),
                (byte)rnd.Next(256),
                (byte)rnd.Next(256)
            );

            Color complementaryColor = Color.FromArgb(
                255,
                (byte)(255 - primaryColor.R),
                (byte)(255 - primaryColor.G),
                (byte)(255 - primaryColor.B)
            );

            return [primaryColor, complementaryColor];
        }
    }

    public class ProgressBarViewModel : INotifyPropertyChanged
    {
        private int _progress;
        private Brush _bgColor;
        private Brush _fgColor;
        private static Random rnd = new();
        public int Progress
        {
            get => _progress;
            set
            {
                _progress = value;
                OnPropertyChanged(nameof(Progress));
            }
        }

        public Brush BgColor
        {
            get => _bgColor;
            set
            {
                _bgColor = value;
                OnPropertyChanged(nameof(BgColor));
            }
        }        
        public Brush FgColor
        {
            get => _fgColor;
            set
            {
                _fgColor = value;
                OnPropertyChanged(nameof(FgColor));
            }
        }

        public ProgressBarViewModel()
        {
            Progress = 0;
            var colors = ColorGenerator.GenerateHarmoniousColors();
            BgColor = new SolidColorBrush(colors[0]);
            FgColor = new SolidColorBrush(colors[1]);
        }


        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class DancingProgressBarsViewModel : INotifyPropertyChanged
    {
        private int _progressBarCount;
        private CancellationTokenSource _cancellationTokenSource;

        public ObservableCollection<ProgressBarViewModel> ProgressBars { get; set; }

        public int ProgressBarCount
        {
            get => _progressBarCount;
            set
            {
                _progressBarCount = value;
                OnPropertyChanged(nameof(ProgressBarCount));
            }
        }

        public ICommand StartCommand { get; }
        public ICommand ResetCommand { get; }

        public DancingProgressBarsViewModel()
        {
            ProgressBars = new ObservableCollection<ProgressBarViewModel>();
            StartCommand = new RelayCommand(StartProgressBars);
            ResetCommand = new RelayCommand(ResetProgressBars);
        }

        private async void StartProgressBars(object obj)
        {
            ResetProgressBars(null);
            _cancellationTokenSource = new CancellationTokenSource();
            var token = _cancellationTokenSource.Token;

            // Initialize progress bars based on user input
            if(ProgressBarCount < 1)
                ProgressBarCount = 1;
            for (int i = 0; i < ProgressBarCount; i++)
            {
                ProgressBars.Add(new ProgressBarViewModel());
            }

            // Start updating each progress bar asynchronously
            foreach (var progressBar in ProgressBars)
            {
                Task.Run(async () =>
                {
                    Random rand = new Random();
                    while (progressBar.Progress < 100 && !token.IsCancellationRequested)
                    {
                        int numb = rand.Next(1, 5);
                        if (progressBar.Progress + numb >= 100)
                        {
                            progressBar.Progress = 100;
                        }
                        else
                        {
                            progressBar.Progress += numb;
                        }
                        await Task.Delay(rand.Next(100, 300), token);


                    }
                }, token);
            }
        }

        private void ResetProgressBars(object obj)
        {
            _cancellationTokenSource?.Cancel();
            ProgressBars.Clear();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
