
namespace WPFTasks.SimulationArchitecture
{
    public class Simulation
    {
        private InteractorsBase interactorsBase;
        private RepositoriesBase repositoriesBase;
        private SimulationConfig simulationConfig;

        public Simulation(SimulationConfig config)
        {
            this.simulationConfig = config;
            this.interactorsBase = new InteractorsBase(config);
            this.repositoriesBase = new RepositoriesBase(config);
        }

        public void Initialize()
        {
            repositoriesBase.CreateAllRepositories();
            interactorsBase.CreateAllInteractors();

            repositoriesBase.SendOnCreateToAllRepositories();
            interactorsBase.SendOnCreateToAllInteractors();

            repositoriesBase.SendInitializeToAllRepositories();
            interactorsBase.SendInitializeToAllInteractors();

            repositoriesBase.SendOnStartToAllRepositories();
            interactorsBase.SendOnStartToAllInteractors();
        }

        public void Dispose()
        {
            simulationConfig = null!;
            interactorsBase = null!;
            repositoriesBase = null!;
        }
        public T GetRepository<T>() where T : Repository
        {
            return this.repositoriesBase.GetRepository<T>();
        }

        public T GetInteractor<T>() where T : Interactor
        {
            return this.interactorsBase.GetInteractor<T>();
        }
    }
}
