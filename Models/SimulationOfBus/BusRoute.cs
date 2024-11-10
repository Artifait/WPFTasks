using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BusNumber = System.UInt32;

namespace WPFTasks.Models.SimulationOfBus
{
    public class BusRoute
    {
        public BusNumber ForBusOfNumber { get; set; }
        public List<Stop> Stops { get; set; }

        public BusRoute(BusNumber bus, List<Stop> stops)
        {
            Stops = stops;
            ForBusOfNumber = bus;
        }

        public Stop GetNextStop(Stop currentStop)
        {
            int currentIndex = Stops.IndexOf(currentStop);
            return Stops[(currentIndex + 1) % Stops.Count];
        }
    }

}
