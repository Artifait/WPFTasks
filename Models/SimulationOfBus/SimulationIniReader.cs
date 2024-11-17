using IniParser;
using IniParser.Model;
using WPFTasks.Models.SimulationOfBus.Data;
using WPFTasks.Models.SimulationOfBus.Repositories;
using BusNumber = System.UInt32;

namespace WPFTasks.Models.SimulationOfBus
{
    public static class SimulationIniReader
    {
        public static void Parse(SimulationArchitecture.Simulation core, string iniFilePath)
        {
            var parser = new FileIniDataParser();
            IniData data = parser.ReadFile(iniFilePath);

            var busesSection = data["Buses"];
            foreach (var key in busesSection)
            {
                var busData = key.Value.Split(',');
                int maxCapacity = int.Parse(busData[0]);
                int busCount = int.Parse(busData[1]);
                BusNumber busNumber = BusNumber.Parse(busData[2]);
                var routeStops = busData[3].Split("->");

                List<Stop> route = new();

                foreach (var routeStop in routeStops)
                {
                    route.Add(core.GetRepository<StopRepository>().AddStop(routeStop.Trim()));
                }

                core.GetRepository<BusRouteRepository>().AddRoute(busNumber, route);
                var rout = core.GetRepository<BusRouteRepository>().GetRoute(busNumber);
                for (int i = 0; i < busCount; i++)
                {
                    core.GetRepository<BusRepository>().Add(new Bus(busNumber, maxCapacity));
                }
            }
        }
    }
}
