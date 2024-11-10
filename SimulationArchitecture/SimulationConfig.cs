
namespace WPFTasks.SimulationArchitecture
{
    public abstract class SimulationConfig
    {
        public abstract Dictionary<Type, Repository> CreateAllRepositories();
        public abstract Dictionary<Type, Interactor> CreateAllInteractors();

        public void CreateInteractor<T>(Dictionary<Type, Interactor> interactorsMap) where T : Interactor, new()
        {
            var interactor = new T();
            var type = typeof(T);

            interactorsMap[type] = interactor;
        }

        public void CreateRepository<T>(Dictionary<Type, Repository> repositoryMap) where T : Repository, new()
        {
            var interactor = new T();
            var type = typeof(T);

            repositoryMap[type] = interactor;
        }
    }
}
