
namespace WPFTasks.Models
{
    public static class Simulation
    {
        private static SimulationArchitecture.Simulation core;
        
        static Simulation()
        {
            core = new(new SimulationOfBus.Config());
            core.Initialize();
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
