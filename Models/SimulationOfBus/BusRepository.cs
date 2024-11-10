using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WPFTasks.SimulationArchitecture;

namespace WPFTasks.Models.SimulationOfBus
{
    public class BusRepository : Repository
    {
        public List<Bus> WorkBuses { get; set; } = [];
        public List<Bus> ParkingBuses { get; set; } = [];

        public override void OnCreate() { }
        public override void Initialize() { }
        public override void OnStart() { }

    }
}
