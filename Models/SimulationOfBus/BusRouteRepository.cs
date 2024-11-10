using WPFTasks.SimulationArchitecture;
using BusNumber = System.UInt32;

namespace WPFTasks.Models.SimulationOfBus
{
    public class BusRouteRepository : Repository
    {
        public Dictionary<BusNumber, BusRoute> BusRoutes { get; private set; } = [];

        public void RemoveRoute(BusNumber busNumber)
            => BusRoutes.Remove(busNumber);
        public BusRoute? GetRoute(BusNumber busNumber)
            => BusRoutes.TryGetValue(busNumber, out BusRoute? route) == true ? route : null;

        public bool AddRoute(BusNumber busNumber, List<Stop> routeOfListStop)
        {
            var route = GetRoute(busNumber);

            if (route == null)
            {
                route = new BusRoute(busNumber, routeOfListStop);
                BusRoutes[busNumber] = route;
                return true;
            }

            return false;
        }
        public BusRoute ChangeRoute(BusNumber busNumber, List<Stop> routeOfListStop)
        {
            var route = new BusRoute(busNumber, routeOfListStop);

            BusRoutes[busNumber] = route;

            return route;
        }

        public override void OnCreate() { }
        public override void Initialize() { }
        public override void OnStart() { }
    }
}

