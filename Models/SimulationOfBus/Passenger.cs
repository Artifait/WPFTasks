
using BusNumber = System.UInt32;

namespace WPFTasks.Models.SimulationOfBus
{
    public class Passenger
    {
        public Stop StartStop { get; set; }
        public Stop EndStop { get; set; }
        public BusNumber SelectedBus { get; set; }

        public Passenger(Stop startStop, Stop endStop, BusNumber selectedBus)
        {
            StartStop = startStop;
            EndStop = endStop;
            SelectedBus = selectedBus;
        }
        
    }

}
