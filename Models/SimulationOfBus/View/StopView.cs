using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Threading;
using WPFTasks.Models.SimulationOfBus.Data;

namespace WPFTasks.Models.SimulationOfBus.View
{
    public class StopView : INotifyPropertyChanged
    {
        private readonly object locker = new();
        private string _statistic;

        public Stop BaseStop { get; }
        public System.Windows.Point Pos { get; set; }
        public ObservableCollection<PassengerView> Passes { get; } = new();

        public string Statistic
        {
            get => _statistic;
            set
            {
                if (_statistic != value)
                {
                    _statistic = value;
                    OnPropertyChanged(nameof(Statistic));
                }
            }
        }

        public double NextPassengerInterval => BaseStop.NextPassengerInterval;
        public double TimeUntilNextPassenger => BaseStop.TimeUntilNextPassenger;

        public StopView(Stop stop, int x, int y)
        {
            BaseStop = stop;
            Pos = new(x, y);

            BaseStop.OnAddPassenger += OnAddPassenger;
            BaseStop.OnDelPassenger += OnDelPassenger;

            // Подписка на изменения свойств модели Stop
            BaseStop.PropertyChanged += Stop_PropertyChanged;

            // Инициализация состояния
            UpdateViewState();
        }

        private void Stop_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(BaseStop.TimeUntilNextPassenger) or nameof(BaseStop.NextPassengerInterval))
            {
                OnPropertyChanged(e.PropertyName);
            }
        }

        private void UpdateViewState()
        {
            lock (locker)
            {
                Statistic = $"{BaseStop.WaitingPassengers.Count}/{Stop.MaxWaitingPasses}";

                var passengerViews = BaseStop.WaitingPassengers
                    .Select(p => new PassengerView(p))
                    .ToList();

                App.Current.Dispatcher.Invoke(() =>
                {
                    // Убираем только изменившиеся элементы
                    Passes.Clear();
                    foreach (var passengerView in passengerViews)
                    {
                        Passes.Add(passengerView);
                    }
                });
            }
        }

        public void OnAddPassenger(Passenger passenger) => UpdateViewState();

        public void OnDelPassenger(Passenger passenger) => UpdateViewState();

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
