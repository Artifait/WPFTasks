using IniParser;
using IniParser.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using WPFTasks.Models.Simulation;
using WPFTasks.SimulationArchitecture;

namespace WPFTasks.Models.SimulationOfBus
{
    public static class BusSimulation
    {
        public static Simulation simulation;

        static BusSimulation()
        {
            simulation = new();
        }

        public void InitializeSimulation(string iniFilePath)
        {
            var parser = new FileIniDataParser();
            IniData data = parser.ReadFile(iniFilePath);

            var busesSection = data["Buses"];
            foreach (var key in busesSection)
            {
                var busData = key.Value.Split(',');
                int maxCapacity = int.Parse(busData[0]);
                int busCount = int.Parse(busData[1]);
                int busNumber = int.Parse(busData[2]);
                var routeStops = busData[3].Split("->");

                List<Stop> route = [];
                foreach (var routeStop in routeStops)
                {
                    Stops.AddStop(routeStop);
                    var stop = Stops.Where(x => x.Name == routeStop).FirstOrDefault();
                    if (stop == null)
                    {
                        stop = new Stop(routeStop);
                        Stops.Add(stop);
                    }

                    route.Add(stop);
                }

                for (int i = 0; i < busCount; i++)
                {
                    ParkingBuses.Add(new Bus(busNumber, maxCapacity, route));
                }
            }
        }

        public string GetReportOfSimulationState()
        {
            StringBuilder sb = new();

            sb.AppendLine("Stops:");
            foreach (var stop in Stops)
            {
                sb.AppendLine($"- {stop.Name}");
            }

            sb.AppendLine("\nBuses:");
            foreach (var bus in WorkBuses)
            {
                sb.AppendLine($"Bus {bus.Number} (Max Capacity: {bus.MaxCapacity})");
                sb.AppendLine("Route: " + string.Join(" -> ", bus.Route.Select(s => s.Name)));
            }
            return sb.ToString();
        }
    }
}
