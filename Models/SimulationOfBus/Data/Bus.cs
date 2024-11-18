using System.Collections.Concurrent;
using WPFTasks.Models.SimulationOfBus.Repositories;
using BusNumber = System.UInt32;

namespace WPFTasks.Models.SimulationOfBus.Data
{
    public class Bus
    {
        private readonly object locker = new();
        private readonly ConcurrentBag<Passenger> passengers = new(); // Потокобезопасная коллекция

        public BusNumber Number { get; }
        public int MaxCapacity { get; }
        public int CurrentPassengerCount
        {
            get
            {
                lock (locker)
                {
                    return passengers.Count;
                }
            }
        }

        public BusRoute? Route
            => Simulation.GetRepository<BusRouteRepository>().GetRoute(Number);

        public Stop CurrentStop { get; private set; }

        public Stop NextStop
        {
            get
            {
                lock (locker)
                {
                    return Route!.GetNextStop(CurrentStop);
                }
            }
        }

        public event Action<Stop>? OnBusCameToStop;
        public event Action<double>? OnUpdateToNextStopProgress;

        private double toNextStopProgress;

        public Bus(BusNumber number, int maxCapacity)
        {
            Number = number;
            MaxCapacity = maxCapacity;

            if (Route == null)
                throw new InvalidOperationException($"Route for bus number {number} is not found!");

            CurrentStop = Route.First();
            OnBusCameToStop += UnloadPassengers;
            OnBusCameToStop += BoardPassengersOnStop;
        }

        public bool BoardPassenger(Passenger passenger, Stop stop)
        {
            lock (locker)
            {
                if (CurrentPassengerCount >= MaxCapacity) return false;

                passengers.Add(passenger);

                // Потокобезопасное удаление пассажира с остановки
                lock (stop)
                {
                    stop.RemovePassenger(passenger);
                }

                return true;
            }
        }

        private void BoardPassengersOnStop(Stop stop)
        {
            lock (stop)
            {
                var passengersToBoard = stop.WaitingPassengers
                                            .Where(p => p.SelectedBus == Number)
                                            .ToList();

                foreach (var passenger in passengersToBoard)
                {
                    if (!BoardPassenger(passenger, stop))
                        break;
                }
            }
        }

        private void UnloadPassengers(Stop stop)
        {
            lock (locker)
            {
                var disembarking = passengers
                    .Where(p => p.EndStop == stop)
                    .ToList();

                foreach (var passenger in disembarking)
                {
                    passengers.TryTake(out _);
                }
            }
        }

        public async Task StartWorking(CancellationToken cancellationToken)
        {
            CurrentStop = Route!.First();

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                lock (locker)
                {
                    toNextStopProgress = 0;
                }

                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await Task.Delay(30, cancellationToken); // Плавный прогресс

                    lock (locker)
                    {
                        toNextStopProgress += 0.01;
                        OnUpdateToNextStopProgress?.Invoke(toNextStopProgress);

                        if (toNextStopProgress >= 1)
                            break;
                    }
                }

                lock (locker)
                {
                    CurrentStop = NextStop;
                }

                OnBusCameToStop?.Invoke(CurrentStop);

                await Task.Delay(2000, cancellationToken); // Небольшая задержка на остановке
            }
        }
    }
}
