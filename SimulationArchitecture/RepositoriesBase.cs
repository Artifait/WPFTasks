using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFTasks.SimulationArchitecture
{
    public class RepositoriesBase
    {
        private Dictionary<Type, Repository> repositoriesMap;
        private SimulationConfig simulationConfig;

        public RepositoriesBase(SimulationConfig simulationConfig)
        {
            this.simulationConfig = simulationConfig;
        }

        public T GetRepository<T>() where T : Repository
        {
            var type = typeof(T);
            return (T)repositoriesMap[type];
        }

        public void CreateAllRepositories()
        {
            repositoriesMap = simulationConfig.CreateAllRepositories();
        }


        public void SendOnCreateToAllRepositories()
        {
            var allRepositories = repositoriesMap.Values;

            foreach (var repository in allRepositories)
            {
                repository.OnCreate();
            }
        }

        public void SendInitializeToAllRepositories()
        {
            var allRepositories = repositoriesMap.Values;

            foreach (var repository in allRepositories)
            {
                repository.Initialize();
            }
        }

        public void SendOnStartToAllRepositories()
        {
            var allRepositories = repositoriesMap.Values;

            foreach (var repository in allRepositories)
            {
                repository.OnStart();
            }
        }

        public void SendOnDisposeToAllRepositories()
        {
            var allRepositories = repositoriesMap.Values;

            foreach (var repository in allRepositories)
            {
                repository.OnDispose();
            }
        }
    }
}
