
using System.Collections.ObjectModel;
using System.ComponentModel;
using WPFTasks.Models.SimulationOfBus;

namespace WPFTasks.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        public UniqueRouteDisplay DisplayRoute { get; set; }

        public MainViewModel()
        {
            Models.Simulation.Init();
            var busRoutes = Models.Simulation.core.GetRepository<BusRouteRepository>().BusRoutes.Values;
            DisplayRoute = new UniqueRouteDisplay(busRoutes);
        }
    }
}
