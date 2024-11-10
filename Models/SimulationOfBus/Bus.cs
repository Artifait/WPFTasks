using BusNumber = System.UInt32;

namespace WPFTasks.Models.SimulationOfBus
{
    public class Bus
    {
        public BusNumber Number { get; set; }
        public int MaxCapacity { get; set; }
        public int CurrentPassengerCount { get; private set; }
        public BusRoute? Route
        {
            get => .GetRepository<BusRouteRepository>().GetRoute(Number);
        }
        public Stop CurrentStop { get; set; }
        public List<Passenger> Passengers { get; set; }

        public Bus(BusNumber number, int maxCapacity, List<Stop> route)
        {
            Number = number;
            MaxCapacity = maxCapacity;
            Passengers = [];
            CurrentStop = route.First(); // Начальная остановка
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

        public void UnloadPassengers()
        {
            // Выгрузить пассажиров, чей пункт назначения совпадает с текущей остановкой
            var disembarking = Passengers.Where(p => p.EndStop == CurrentStop.Name).ToList();
            foreach (var passenger in disembarking)
            {
                Passengers.Remove(passenger);
                CurrentPassengerCount--;
            }
        }
    }

}
