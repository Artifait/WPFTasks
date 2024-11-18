using WPFTasks.Models.SimulationOfBus.Data;
using WPFTasks.SimulationArchitecture;
using BusNumber = System.UInt32;

namespace WPFTasks.Models.SimulationOfBus.Repositories
{
    public class BusRepository : Repository
    {
        public List<Bus> WorkBuses { get; private set; } = [];
        public List<Bus> ParkingBuses { get; private set; } = [];
        public Dictionary<BusNumber, uint> BusOfNumberCount { get; private set; } = [];

        object locker = new();

        public void AllParkGoWork()
        {
            lock(locker)
            {
                WorkBuses.AddRange(ParkingBuses);
                ParkingBuses.Clear();
            }
        }

        public void Add(Bus bus)
        {
            lock(locker)
            {
                ParkingBuses.Add(bus);

                if (BusOfNumberCount.ContainsKey(bus.Number))
                {
                    BusOfNumberCount[bus.Number]++;
                }
                else
                {
                    BusOfNumberCount[bus.Number] = 1;
                }
            }
        }
        public Bus? SwapParkToWork(BusNumber num)
        {
            var bus = GetParkingBus(num);

            lock (locker)
            {
                if (bus != null)
                {
                    ParkingBuses.Remove(bus);
                    WorkBuses.Add(bus);
                }
            }
            
            return bus;
        }
        public void SwapWorkToPark(Bus bus)
        {
            lock (locker)
            {
                WorkBuses.Remove(bus);
                ParkingBuses.Add(bus);
            }
        }

        public Bus? GetParkingBus(BusNumber num)
        {
            lock (locker)
            {
                return ParkingBuses.Where(b => b.Number == num).FirstOrDefault();
            }
        }
       

        public override void OnCreate() { }
        public override void Initialize() { }
        public override void OnStart() { }

        public override void OnDispose()
        {
            lock (locker)
            {
                WorkBuses = null!;
                ParkingBuses = null!;
                BusOfNumberCount = null!;
            }
        }
    }
}
