
namespace WPFTasks.Models.SimulationOfBus
{
    public class Config : SimulationArchitecture.SimulationConfig
    {
        public override Dictionary<Type, SimulationArchitecture.Interactor> CreateAllInteractors()
        {
            var interactorsMap = new Dictionary<Type, SimulationArchitecture.Interactor>();

            //CreateInteractor<CoinInteractor>(interactorsMap);
            return interactorsMap;
        }

        public override Dictionary<Type, SimulationArchitecture.Repository> CreateAllRepositories()
        {
            var repositoriesMap = new Dictionary<Type, SimulationArchitecture.Repository>();

            CreateRepository<BusRepository>(repositoriesMap);
            CreateRepository<BusRouteRepository>(repositoriesMap);
            CreateRepository<StopRepository>(repositoriesMap);

            return repositoriesMap;
        }
    }
}
