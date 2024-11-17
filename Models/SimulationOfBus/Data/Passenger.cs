using System;
using WPFTasks.Models.SimulationOfBus.Repositories;
using BusNumber = System.UInt32;

namespace WPFTasks.Models.SimulationOfBus.Data
{
    public class Passenger
    {
        public Stop StartStop { get; set; }
        public Stop EndStop { get; set; }
        public BusNumber SelectedBus { get; set; }

        public Passenger(Stop startStop, Stop endStop, BusNumber selectedBus)
        {
            StartStop = startStop;
            EndStop = endStop;
            SelectedBus = selectedBus;
        }


        private static BusRouteRepository repOfRoute = Simulation.GetRepository<BusRouteRepository>();
        private static BusRepository repOfBus = Simulation.GetRepository<BusRepository>();
        private static Random rnd = new();

        public static Passenger GenerateRnd(Stop start)
        {
            var keys = repOfBus.BusOfNumberCount.Keys.Where(k => repOfRoute.GetRoute(k).Stops.IndexOf(start) >= 0).ToList();

            if (keys.Count == 0)
                throw new InvalidOperationException("No buses available.");

            uint busNum = keys[rnd.Next(keys.Count)];

            var route = repOfRoute.GetRoute(busNum);
            if (route == null)
                throw new InvalidOperationException($"Route not found for bus number {busNum}.");

            var stops = route.Stops.Where(g => g != start).ToList();
            if (stops.Count == 0)
                throw new InvalidOperationException($"No valid stops available for the route of bus {busNum}.");

            var end = stops[rnd.Next(stops.Count)];

            return new Passenger(start, end, busNum);
        }

    }
}
