
using System.ComponentModel;
using Timer = System.Timers.Timer;

namespace WPFTasks.Models.SimulationOfBus.Data
{
    public class Stop : INotifyPropertyChanged
    {
        private static Random rnd = new();
        private Timer timer;
        private Timer countdownTimer;

        private double _timeUntilNextPassenger;
        public double TimeUntilNextPassenger
        {
            get => _timeUntilNextPassenger;
            set
            {
                if (_timeUntilNextPassenger != value)
                {
                    _timeUntilNextPassenger = value;
                    OnPropertyChanged(nameof(TimeUntilNextPassenger));
                }
            }
        }

        public double NextPassengerInterval { get; private set; } = 10.0; // Время между пассажирами в секундах

        public string Name { get; set; }
        public List<Passenger> WaitingPassengers { get; set; }
        public Action<Passenger> OnAddPassenger;

        public Stop(string name)
        {
            Name = name;
            WaitingPassengers = new List<Passenger>();
            StartPassengerGeneration();
        }

        public void AddPassenger(Passenger passenger)
        {
            WaitingPassengers.Add(passenger);
            OnAddPassenger?.Invoke(passenger);
        }

        private void StartPassengerGeneration()
        {
            timer = new Timer();
            timer.Elapsed += (s, e) => GeneratePassenger();
            ScheduleNextPassenger();

            // Таймер для обновления прогресс-бара каждую секунду
            countdownTimer = new Timer(1000);
            countdownTimer.Elapsed += (s, e) => UpdateCountdown();
            countdownTimer.Start();
        }

        private void GeneratePassenger()
        {
            timer.Stop();

            var passenger = Passenger.GenerateRnd(this);
            AddPassenger(passenger);

            ScheduleNextPassenger();
        }

        private void ScheduleNextPassenger()
        {
            NextPassengerInterval = rnd.Next(1, 2); // Интервал в секундах (1-2 секунд)
            TimeUntilNextPassenger = NextPassengerInterval;

            timer.Interval = NextPassengerInterval * 1000;
            timer.Start();
        }

        private void UpdateCountdown()
        {
            if (TimeUntilNextPassenger > 0)
            {
                TimeUntilNextPassenger -= 1;
            }
            else
            {
                TimeUntilNextPassenger = 0;
            }
        }

        public void StopPassengerGeneration()
        {
            timer?.Stop();
            timer?.Dispose();
            countdownTimer?.Stop();
            countdownTimer?.Dispose();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public override bool Equals(object obj)
        {
            if (obj is Stop otherStop)
            {
                return Name == otherStop.Name;
            }
            return false;
        }

        public override int GetHashCode()
            => Name != null ? Name.GetHashCode() : 0;

        public static bool operator ==(Stop left, Stop right)
        {
            if (ReferenceEquals(left, null))
                return ReferenceEquals(right, null);
            return left.Equals(right);
        }

        public static bool operator !=(Stop left, Stop right)
            => !(left == right);
    }
}
