
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using WPFTasks.Models.SimulationOfBus.Data;
using WPFTasks.Models.SimulationOfBus.Intreractors;
using WPFTasks.Models.SimulationOfBus.Repositories;
using WPFTasks.Models.SimulationOfBus.View;

namespace WPFTasks.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        public UniqueRouteDisplay DisplayRoute { get; set; }
        public ObservableCollection<BusView> Buss { get; set; } = [];
        private BusInteractor interOfBus = null!;
        private BusRouteRepository repOfRoute = null!;
        private CancellationTokenRegistration ctg = new();
        public MainViewModel()
        {
            Models.Simulation.Init();
            repOfRoute = Models.Simulation.GetRepository<BusRouteRepository>();
            interOfBus = Models.Simulation.GetInteractor<BusInteractor>();

            var busRoutes = repOfRoute.BusRoutes.Values;
            DisplayRoute = new UniqueRouteDisplay(busRoutes);
            interOfBus.StartAllBuses();
            interOfBus.OnStartBus += OnStartBus;

        }
        public void OnStartBus(Bus bus)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                Buss.Add(new(bus));
            });
        }
    }
}
