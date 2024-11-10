using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFTasks.Models.SimulationOfBus
{
    public class Stop
    {
        public string Name { get; set; }
        public List<Passenger> WaitingPassengers { get; set; }

        public Stop(string name)
        {
            Name = name;
            WaitingPassengers = new List<Passenger>();
        }

        public void AddPassenger(Passenger passenger)
        {
            WaitingPassengers.Add(passenger);
        }
    }

}
