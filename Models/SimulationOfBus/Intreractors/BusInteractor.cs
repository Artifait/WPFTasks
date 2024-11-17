
using Microsoft.Identity.Client;
using WPFTasks.Models.SimulationOfBus.Data;
using WPFTasks.Models.SimulationOfBus.Repositories;
using WPFTasks.SimulationArchitecture;
using BusNumber = System.UInt32;

namespace WPFTasks.Models.SimulationOfBus.Intreractors
{
    public class BusInteractor : Interactor
    {
        private BusRepository rep = null!;
        private CancellationTokenRegistration ctg = new();

        public Action<Bus> OnStartBus;
        public override void OnStart()
            => rep = WPFTasks.Models.Simulation.GetRepository<BusRepository>();

        public async void StartBussOfNumber(BusNumber num)
        {
            if (!rep.BusOfNumberCount.TryGetValue(num, out uint cnt))
                cnt = 0;
            Bus bus = null!;

            void StartNextBus()
            {
                if (cnt == 0)
                    return;

                bus = rep.SwapParkToWork(num)!;
                bus.OnBusCameToStop += ComeToStop;

                StartBus(bus);
            }

            void ComeToStop(Stop _)
            {
                cnt--;
#pragma warning disable CS8601 
                bus.OnBusCameToStop -= ComeToStop;
#pragma warning restore CS8601 
                StartNextBus();
            }

            if (cnt > 0)
            {
                StartNextBus();
            }
        }

        public void StartAllBuses()
        {
            List<Task> tasks = [];

            foreach(var num in rep.BusOfNumberCount.Keys)
            {
                tasks.Add(Task.Run(() => { StartBussOfNumber(num); }));
            }
        }


        private async void StartBus(Bus bus)
        {
            OnStartBus?.Invoke(bus);
            await bus.StartWorking(ctg.Token);
        }
    }
}
