
using System.Windows.Media;

namespace WPFTasks.Models
{
    public static class Simulation
    {
        public static SimulationOfBus.BusSimulation core = new();

        public static void Init()
        {
            core.InitializeSimulation("M:\\JournalTop\\ADO.NET\\WPFTasks\\SimulationConfig.ini");
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
