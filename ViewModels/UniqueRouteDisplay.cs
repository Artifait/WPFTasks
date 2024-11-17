
using WPFTasks.Models.SimulationOfBus;
using System.Linq;

namespace WPFTasks.ViewModels
{
    public class UniqueRouteDisplay
    {
        private static readonly Random Random = new Random();

        public List<StopDisplay> Stops { get; set; }
        public List<(StopDisplay Start, StopDisplay End)> Links { get; set; }

        public UniqueRouteDisplay(IEnumerable<BusRoute> busRoutes)
        {
            Stops = [];
            Links = [];

            // Собираем уникальные остановки
            var uniqueStops = busRoutes
                .SelectMany(route => route.Stops)
                .Distinct()
                .ToList();

            // Создаем StopDisplay для каждой уникальной остановки
            foreach (var stop in uniqueStops)
            {
                Stops.Add(new StopDisplay(stop, Random.Next(50, 700), Random.Next(50, 400)));
            }

            // Сопоставляем остановки из маршрутов с StopDisplay
            var stopDisplayMap = Stops.ToDictionary(s => s.BaseStop, s => s);

            // Формируем ссылки (Links)
            foreach (var route in busRoutes)
            {
                var routeStops = route.Stops
                    .Select(stop => stopDisplayMap[stop])
                    .ToList();

                for (int i = 0; i < routeStops.Count; i++)
                {
                    // Добавляем связь между соседними остановками
                    if (i < routeStops.Count - 1)
                    {
                        Links.Add((routeStops[i], routeStops[i + 1]));
                    }

                    // Добавляем связь между первой и последней остановкой (если их больше 2)
                    if (routeStops.Count > 2 && i == routeStops.Count - 1)
                    {
                        Links.Add((routeStops[i], routeStops[0]));
                    }
                }
            }
        }
    }
}
