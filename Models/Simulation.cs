
using System.Windows;
using System.Windows.Media;
using WPFTasks.Models.SimulationOfBus.Repositories;
using WPFTasks.ViewModels;
using BusNumber = System.UInt32;

namespace WPFTasks.Models
{
    public static class Simulation
    {
        public static SimulationOfBus.BusSimulation core = null!;
        public static Dictionary<BusNumber, SolidColorBrush> Brushes { get; set; } = null!;
        public static void Init()
        {
            Dispose();
            core = new();
            core.InitializeSimulation("../../../SimulationConfig.ini");
            Brushes = [];
            foreach (var num in GetRepository<BusRepository>().BusOfNumberCount.Keys)
            {
                Brushes[num] = new SolidColorBrush(ColorGenerator.GenerateColor());
            }
        }
        public static void Dispose()
        {
            if(core != null)
            {
                core.Dispose();
            }

        }
        public static T GetRepository<T>() where T : SimulationArchitecture.Repository
        {
            return core.GetRepository<T>();
        }

        public static T GetInteractor<T>() where T : SimulationArchitecture.Interactor
        {
            return core.GetInteractor<T>();
        }
    }
}
