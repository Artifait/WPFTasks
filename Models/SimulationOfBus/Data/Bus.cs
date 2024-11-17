using WPFTasks.Models.SimulationOfBus.Repositories;
using BusNumber = System.UInt32;

namespace WPFTasks.Models.SimulationOfBus.Data
{
    public class Bus
    {
        public BusNumber Number { get; set; }
        public int MaxCapacity { get; set; }
        public int CurrentPassengerCount { get; private set; }
        public BusRoute? Route
        {
            get => Simulation.GetRepository<BusRouteRepository>().GetRoute(Number);
        }
        public Stop CurrentStop { get; set; }
        public Stop NextStop => Route!.GetNextStop(CurrentStop);
        public List<Passenger> Passengers { get; set; }
        public Action<Stop> OnBusCameToStop;
        public Action<double> OnUpdateToNextStopProgress;
        private double ToNextStopProgress;

        public Bus(BusNumber number, int maxCapacity)
        {
            Number = number;
            MaxCapacity = maxCapacity;
            Passengers = [];
            CurrentStop = Route!.First();

            OnBusCameToStop += UnloadPassengers;
        }

        public bool BoardPassenger(Passenger passenger)
        {
            if (CurrentPassengerCount < MaxCapacity)
            {
                Passengers.Add(passenger);
                CurrentPassengerCount++;
                return true;
            }
            return false;
        }
        private void BoardPassengersOnStop(Stop stop)
        {
            foreach (var p in stop.WaitingPassengers)
            {
                if(p.SelectedBus == Number)
                {
                    if(!BoardPassenger(p))
                        return;
                }
            }
        }
        private void UnloadPassengers(Stop stop)
        {
            var disembarking = Passengers.Where(p => p.EndStop == stop).ToList();

            foreach (var passenger in disembarking)
            {
                Passengers.Remove(passenger);
                CurrentPassengerCount--;
            }
        }

        public async Task StartWorking(CancellationToken cancellationToken)
        {
            CurrentStop = Route!.First();

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                ToNextStopProgress = 0;

                while (ToNextStopProgress < 1)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await Task.Delay(30, cancellationToken); // Уменьшена задержка

                    ToNextStopProgress += 0.01; // Меньший шаг для плавного движения
                    OnUpdateToNextStopProgress?.Invoke(ToNextStopProgress);
                }

                CurrentStop = NextStop;
                OnBusCameToStop?.Invoke(CurrentStop);
                UnloadPassengers(CurrentStop);
                BoardPassengersOnStop(CurrentStop);

                await Task.Delay(2000, cancellationToken); // Переход к следующей остановке с небольшой задержкой
            }
        }

    }
}
