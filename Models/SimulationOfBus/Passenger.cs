using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFTasks.Models.SimulationOfBus
{
    public class Passenger
    {
        public string StartStop { get; set; }
        public string EndStop { get; set; }
        public Bus SelectedBus { get; set; }

        public Passenger(string startStop, string endStop, Bus selectedBus)
        {
            StartStop = startStop;
            EndStop = endStop;
            SelectedBus = selectedBus;
        }
    }

}
