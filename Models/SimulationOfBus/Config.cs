using WPFTasks.Models.SimulationOfBus.Intreractors;
using WPFTasks.Models.SimulationOfBus.Repositories;

namespace WPFTasks.Models.SimulationOfBus
{
    public class Config : SimulationArchitecture.SimulationConfig
    {
        public override Dictionary<Type, SimulationArchitecture.Interactor> CreateAllInteractors()
        {
            var interactorsMap = new Dictionary<Type, SimulationArchitecture.Interactor>();

            CreateInteractor<BusInteractor>(interactorsMap);
            return interactorsMap;
        }

        public override Dictionary<Type, SimulationArchitecture.Repository> CreateAllRepositories()
        {
            var repositoriesMap = new Dictionary<Type, SimulationArchitecture.Repository>();

            CreateRepository<BusRepository>(repositoriesMap);
            CreateRepository<BusRouteRepository>(repositoriesMap);
            CreateRepository<StopRepository>(repositoriesMap);
            CreateRepository<StopDisplayRepository>(repositoriesMap);

            return repositoriesMap;
        }
    }
}
