
using System.Collections.ObjectModel;
using System.ComponentModel;
using WPFTasks.Models.SimulationOfBus;

namespace WPFTasks.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        public ObservableCollection<RouteDisplay> Routes { get; set; }
        public Dictionary<string, StopDisplay> AllStops { get; set; }

        public MainViewModel()
        {
            AllStops = new Dictionary<string, StopDisplay>();
            var busRoutes = Models.Simulation.core.GetRepository<BusRouteRepository>().BusRoutes.Values;

            Routes = new ObservableCollection<RouteDisplay>(
                busRoutes.Select(route => new RouteDisplay(route, AllStops))
            );
        }
    }
}
