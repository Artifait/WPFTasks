
using System.Collections.ObjectModel;
using WPFTasks.Models.SimulationOfBus;

namespace WPFTasks.ViewModels
{
    public class RouteDisplay
    {
        public BusRoute BaseRoute { get; }
        public List<StopDisplay> StopDisplays { get; set; }

        public RouteDisplay(BusRoute route, Dictionary<string, StopDisplay> allStops)
        {
            BaseRoute = route;
            StopDisplays = new List<StopDisplay>();

            foreach (var stop in route.Stops)
            {
                // Если остановка уже существует в allStops, используем её координаты
                if (!allStops.ContainsKey(stop.Name))
                {
                    var random = new Random();
                    double x = random.Next(50, 700);  // Генерируем случайные координаты
                    double y = random.Next(50, 400);
                    allStops[stop.Name] = new StopDisplay(stop, x, y);
                }
                StopDisplays.Add(allStops[stop.Name]);
            }
        }
    }
}
