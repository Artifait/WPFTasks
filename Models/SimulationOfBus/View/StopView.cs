using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Threading;
using WPFTasks.Models.SimulationOfBus.Data;

namespace WPFTasks.Models.SimulationOfBus.View
{
    public class StopView : INotifyPropertyChanged
    {
        public Stop BaseStop { get; }
        public System.Windows.Point Pos { get; set; }
        public ObservableCollection<PassengerView> Passes { get; set; } = new();

        public double NextPassengerInterval => BaseStop.NextPassengerInterval;
        public double TimeUntilNextPassenger => BaseStop.TimeUntilNextPassenger;

        public StopView(Stop stop, int x, int y)
        {
            BaseStop = stop;
            Pos = new(x, y);
            BaseStop.OnAddPassenger += OnAddPassenger;

            // Подписка на изменения свойств модели Stop
            BaseStop.PropertyChanged += Stop_PropertyChanged;
        }

        private void Stop_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BaseStop.TimeUntilNextPassenger) ||
                e.PropertyName == nameof(BaseStop.NextPassengerInterval))
            {
                OnPropertyChanged(e.PropertyName);
            }
        }

        public void OnAddPassenger(Passenger passenger)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                Passes.Add(new PassengerView(passenger));
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
