using System.Text;

namespace WPFTasks.Models.SimulationOfBus
{
    public class BusSimulation
    {
        public SimulationArchitecture.Simulation core;

        public BusSimulation()
        {
            core = new(new Config());
            core.Initialize();
        }

        public void InitializeSimulation(string iniFilePath)
            => SimulationIniReader.Parse(core, iniFilePath);

        public string GetReportOfSimulationState()
        {
            StringBuilder sb = new();

            sb.AppendLine("Stops:");
            foreach (var stop in core.GetRepository<StopRepository>().StopList)
            {
                sb.AppendLine($"- {stop.Name}");
            }

            sb.AppendLine("\nParking Buses:");
            foreach (var bus in core.GetRepository<BusRepository>().ParkingBuses)
            {
                sb.AppendLine($"Bus {bus.Number} (Max Capacity: {bus.MaxCapacity})");
                sb.AppendLine("Route: " + string.Join(" -> ", bus.Route!.Stops.Select(s => s.Name)));
            }

            sb.AppendLine("\nWorking Buses:");
            foreach (var bus in core.GetRepository<BusRepository>().WorkBuses)
            {
                sb.AppendLine($"Bus {bus.Number} (Max Capacity: {bus.MaxCapacity})");
                sb.AppendLine("Route: " + string.Join(" -> ", bus.Route!.Stops.Select(s => s.Name)));
            }

            return sb.ToString();
        }

        public T GetRepository<T>() where T : SimulationArchitecture.Repository
        {
            return core.GetRepository<T>();
        }

        public T GetInteractor<T>() where T : SimulationArchitecture.Interactor
        {
            return core.GetInteractor<T>();
        }
    }
}
