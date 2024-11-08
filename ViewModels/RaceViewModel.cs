
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace WPFTasks.ViewModels
{
    public class HorseRaceViewModel : INotifyPropertyChanged
    {
        private readonly Random _random = new Random();
        private CancellationTokenSource _cancellationTokenSource;
        private bool _raceInProgress;
        private int _numberPlace = 1;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<HorseViewModel> ProgressBars { get; set; }
        public ICommand StartCommand { get; }
        public ICommand ResetCommand { get; }

        public HorseRaceViewModel()
        {
            ProgressBars = new ObservableCollection<HorseViewModel>();
            for (int i = 1; i <= 5; i++)
            {
                ProgressBars.Add(new HorseViewModel { HorseName = $"Horse {i}" });
            }
            StartCommand = new RelayCommand(StartRace, (o) => { return !_raceInProgress; });
            ResetCommand = new RelayCommand(ResetRace, (o) => !_raceInProgress);
        }

        private async void StartRace(object obj)
        {
            if (_raceInProgress) return;
            _raceInProgress = true;

            _cancellationTokenSource = new CancellationTokenSource();
            var tasks = new Task[ProgressBars.Count];

            for (int i = 0; i < ProgressBars.Count; i++)
            {
                var horse = ProgressBars[i];
                horse.ResetProgress();
                tasks[i] = RunHorseRaceAsync(horse, _cancellationTokenSource.Token);
            }

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _raceInProgress = false;
                _numberPlace = 1;
            }
        }
        private object locker = new();
        private async Task RunHorseRaceAsync(HorseViewModel horse, CancellationToken cancellationToken)
        {
            while (horse.Progress < 100)
            {
                cancellationToken.ThrowIfCancellationRequested();
                horse.Progress += _random.Next(1, 10); 
                await Task.Delay(100, cancellationToken);
            }
            lock(locker)
            {
                MessageBox.Show($"Horse: {horse.HorseName} на {_numberPlace++} месте.");
            }
            horse.Progress = 100; 
        }

        private void ResetRace(object obj)
        {
            _cancellationTokenSource?.Cancel();
            foreach (var horse in ProgressBars)
            {
                horse.ResetProgress();
            }
        }
    }

    public class HorseViewModel : INotifyPropertyChanged
    {
        private double _progress;
        public string HorseName { get; set; }
        public double Progress
        {
            get => _progress;
            set
            {
                _progress = value;
                OnPropertyChanged(nameof(Progress));
            }
        }

        public SolidColorBrush BgColor => new SolidColorBrush(Colors.LightGray);
        public SolidColorBrush FgColor => new SolidColorBrush(Colors.Green);

        public event PropertyChangedEventHandler PropertyChanged;

        public void ResetProgress()
        {
            Progress = 0;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
