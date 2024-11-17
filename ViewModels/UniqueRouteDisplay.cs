using System.Linq;
using System.Collections.ObjectModel;
using WPFTasks.Models.SimulationOfBus.Data;
using WPFTasks.Models.SimulationOfBus.View;
using WPFTasks.Models.SimulationOfBus.Repositories;

namespace WPFTasks.ViewModels
{
    public class Link
    {
        public static System.Windows.Point Sum(System.Windows.Point point1, System.Windows.Point point2)
        {
            return new System.Windows.Point(point1.X + point2.X, point1.Y + point2.Y);
        }

        public Link(StopView start, StopView end)
        {
            Start = Sum(start.Pos, new(10, 10));
            End = Sum(end.Pos, new(10, 10));
        }

        public System.Windows.Point Start { get; set; }
        public System.Windows.Point End { get; set; }
    }

    public class UniqueRouteDisplay
    {
        private static readonly Random Random = new Random();
        private static StopDisplayRepository rep = Models.Simulation.GetRepository<StopDisplayRepository>();

        public ObservableCollection<StopView> Stops { get; set; }
        public ObservableCollection<Link> Links { get; set; }

        public UniqueRouteDisplay(IEnumerable<BusRoute> busRoutes)
        {
            Stops = [];
            Links = [];

            // Собираем уникальные остановки
            var uniqueStops = busRoutes
                .SelectMany(route => route.Stops)
                .Distinct()
                .ToList();

            // Определяем радиус для кругового распределения (можно варьировать по необходимости)
            double centerX = 350, centerY = 250; // Центр координат
            double radius = 200; // Радиус круга

            // Равномерно распределяем остановки по окружности
            int numberOfStops = uniqueStops.Count;
            double angleStep = 2 * Math.PI / numberOfStops;

            // Создаем StopDisplay для каждой уникальной остановки
            for (int i = 0; i < numberOfStops; i++)
            {
                // Вычисляем угол для каждой остановки
                double angle = i * angleStep;
                double x = centerX + radius * Math.Cos(angle); // Вычисляем X координату
                double y = centerY + radius * Math.Sin(angle); // Вычисляем Y координату
                var sd = new StopView(uniqueStops[i], (int)x, (int)y);

                Stops.Add(sd);
                rep.AddStop(sd);
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
                        Links.Add(new(routeStops[i], routeStops[i + 1]));
                    }

                    // Добавляем связь между первой и последней остановкой (если их больше 2)
                    if (routeStops.Count > 2 && i == routeStops.Count - 1)
                    {
                        Links.Add(new(routeStops[i], routeStops[0]));
                    }
                }
            }
        }
    }
}
