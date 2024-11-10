
using WPFTasks.SimulationArchitecture;

namespace WPFTasks.Models.SimulationOfBus
{
    public class SimulationConfig : SimulationArchitecture.SimulationConfig
    {
        public override Dictionary<Type, Interactor> CreateAllInteractors()
        {
            var interactorsMap = new Dictionary<Type, Interactor>();

            //CreateInteractor<CoinInteractor>(interactorsMap);
            return interactorsMap;
        }

        public override Dictionary<Type, Repository> CreateAllRepositories()
        {
            var repositoriesMap = new Dictionary<Type, Repository>();

            CreateRepository<BusRepository>(repositoriesMap);
            CreateRepository<BusRouteRepository>(repositoriesMap);
            CreateRepository<StopRepository>(repositoriesMap);

            return repositoriesMap;
        }
    }
}
