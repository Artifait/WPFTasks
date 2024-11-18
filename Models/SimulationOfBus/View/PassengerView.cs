using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using WPFTasks.Models.SimulationOfBus.Data;
using WPFTasks.Models.SimulationOfBus.Repositories;

namespace WPFTasks.Models.SimulationOfBus.View
{
    public class PassengerView
    {
        public Passenger BasePassenger { get; set; }
        public SolidColorBrush Filler => Simulation.Brushes[BasePassenger.SelectedBus];

        public PassengerView(Passenger passenger)
        {
            BasePassenger = passenger;
        }
    }
}
