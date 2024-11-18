using System.Collections.Concurrent;
using System.ComponentModel;
using Timer = System.Timers.Timer;

namespace WPFTasks.Models.SimulationOfBus.Data
{
    public class Stop : INotifyPropertyChanged
    {
        private static readonly Random rnd = new();
        private readonly Timer timer;
        private readonly Timer countdownTimer;
        public static int MaxWaitingPasses = 50;

        private readonly object locker = new();

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
        public string Name { get; }
        public ConcurrentBag<Passenger> WaitingPassengers { get; } = new();
        public Action<Passenger>? OnAddPassenger;
        public Action<Passenger>? OnDelPassenger;
        public static readonly int MaxWaitingPassengers = 50;

        public Stop(string name)
        {
            Name = name;

            // Настройка таймера для генерации пассажиров
            timer = new Timer();
            timer.Elapsed += (_, _) => GeneratePassenger();
            ScheduleNextPassenger();

            // Таймер для обновления прогресс-бара каждую секунду
            countdownTimer = new Timer(1000);
            countdownTimer.Elapsed += (_, _) => UpdateCountdown();
            countdownTimer.Start();
        }

        public void AddPassenger(Passenger passenger)
        {
            lock (locker)
            {
                if (WaitingPassengers.Count >= MaxWaitingPassengers) return;
                WaitingPassengers.Add(passenger);
                OnAddPassenger?.Invoke(passenger);
            }
        }

        public void RemovePassenger(Passenger passenger)
        {
            lock (locker)
            {
                if (WaitingPassengers.TryTake(out passenger))
                {
                    OnDelPassenger?.Invoke(passenger);
                }
            }
        }

        private void StartPassengerGeneration()
        {
            timer.Start();
            countdownTimer.Start();
        }

        private void GeneratePassenger()
        {
            lock (locker)
            {
                if (WaitingPassengers.Count >= MaxWaitingPassengers) return;

                var passenger = Passenger.GenerateRnd(this);
                AddPassenger(passenger);

                ScheduleNextPassenger();
            }
        }

        private void ScheduleNextPassenger()
        {
            lock (locker)
            {
                NextPassengerInterval = rnd.Next(2, 10); // Интервал в секундах (2-10 секунд)
                TimeUntilNextPassenger = NextPassengerInterval;

                timer.Interval = NextPassengerInterval * 1000;
                timer.Start();
            }
        }

        private void UpdateCountdown()
        {
            lock (locker)
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
        }

        public void StopPassengerGeneration()
        {
            timer?.Stop();
            countdownTimer?.Stop();
            timer?.Dispose();
            countdownTimer?.Dispose();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public override bool Equals(object obj)
            => obj is Stop otherStop && Name == otherStop.Name;

        public override int GetHashCode()
            => Name.GetHashCode();

        public static bool operator ==(Stop left, Stop right)
            => ReferenceEquals(left, null) ? ReferenceEquals(right, null) : left.Equals(right);

        public static bool operator !=(Stop left, Stop right)
            => !(left == right);
    }
}
