
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Media;
using WPFTasks.Models.SimulationOfBus.Data;
using WPFTasks.Models.SimulationOfBus.Intreractors;
using WPFTasks.Models.SimulationOfBus.Repositories;
using WPFTasks.Models.SimulationOfBus.View;
using BusNumber = System.UInt32;

namespace WPFTasks.ViewModels
{
    public class InfoOfBus : BaseViewModel
    {
        private SolidColorBrush _filler = null!;
        private BusNumber _number;
        private uint _count;

        public SolidColorBrush Filler { get => _filler; set => SetProperty(ref _filler, value); }
        public BusNumber Number { get => _number; set => SetProperty(ref _number, value); }
        public uint Count { get => _count; set => SetProperty(ref _count, value); }

        public InfoOfBus(BusNumber num) 
        {
            Filler = Models.Simulation.Brushes[num];
            Count = rep.BusOfNumberCount[num];
            Number = num;
        }

        private static BusRepository rep = null!;
        public static void Init() => rep = Models.Simulation.GetRepository<BusRepository>();
    }
    public class MainViewModel : BaseViewModel
    {
        public UniqueRouteDisplay DisplayRoute { get; set; }
        public ObservableCollection<BusView> Buss { get; set; } = [];
        public ObservableCollection<InfoOfBus> InfoOfBuses { get; set; } = [];

        private BusInteractor interOfBus = null!;
        private BusRouteRepository repOfRoute = null!;
        private CancellationTokenRegistration ctg = new();
        public MainViewModel()
        {
            Models.Simulation.Init();
            InfoOfBus.Init();

            repOfRoute = Models.Simulation.GetRepository<BusRouteRepository>();
            interOfBus = Models.Simulation.GetInteractor<BusInteractor>();
            var repOfBus = Models.Simulation.GetRepository<BusRepository>();

            foreach (var num in repOfBus.BusOfNumberCount.Keys)
                InfoOfBuses.Add(new(num));
            
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
