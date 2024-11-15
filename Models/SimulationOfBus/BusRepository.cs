
using WPFTasks.SimulationArchitecture;
using BusNumber = System.UInt32;

namespace WPFTasks.Models.SimulationOfBus
{
    public class BusRepository : Repository
    {
        public List<Bus> WorkBuses { get; private set; } = [];
        public List<Bus> ParkingBuses { get; private set; } = [];
        public Dictionary<BusNumber, uint> BusOfNumberCount { get; private set; } = [];

        public override void OnCreate() { }
        public override void Initialize() { }
        public override void OnStart() { }

        public void Add(Bus bus)
        {
            ParkingBuses.Add(bus);

            if(BusOfNumberCount.ContainsKey(bus.Number)) {
                BusOfNumberCount[bus.Number]++;
            } else { 
                BusOfNumberCount[bus.Number] = 1;
            }
            
        }

        public Bus? SwapParkToWork(BusNumber num)
        {
            var bus = GetParkingBus(num);

            if(bus != null)
            {
                ParkingBuses.Remove(bus);
                WorkBuses.Add(bus);
            }

            return bus;
        }

        public void SwapWorkToPark(Bus bus)
        {
            WorkBuses.Remove(bus);
            ParkingBuses.Add(bus);
        }

        public Bus? GetParkingBus(BusNumber num)
            => ParkingBuses.Where(b => b.Number == num).FirstOrDefault();

    }
}
