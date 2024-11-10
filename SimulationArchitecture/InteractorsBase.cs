using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFTasks.SimulationArchitecture
{
    public class InteractorsBase
    {
        private Dictionary<Type, Interactor> interactorsMap;
        private SimulationConfig simulationConfig;

        public InteractorsBase(SimulationConfig sceneConfig)
        {
            simulationConfig = sceneConfig;
        }

        public T GetInteractor<T>() where T : Interactor
        {
            var type = typeof(T);
            return (T)interactorsMap[type];
        }

        public void CreateAllInteractors()
        {
            interactorsMap = simulationConfig.CreateAllInteractors();
        }


        public void SendOnCreateToAllInteractors()
        {
            var allInteractors = interactorsMap.Values;

            foreach (var interactor in allInteractors)
            {
                interactor.OnCreate();
            }
        }

        public void SendInitializeToAllInteractors()
        {
            var allInteractors = interactorsMap.Values;

            foreach (var interactor in allInteractors)
            {
                interactor.Initialize();
            }
        }

        public void SendOnStartToAllInteractors()
        {
            var allInteractors = interactorsMap.Values;

            foreach (var interactor in allInteractors)
            {
                interactor.OnStart();
            }
        }
    }
}