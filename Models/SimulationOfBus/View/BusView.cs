
using System.Windows;
using System.Windows.Media;
using WPFTasks.Models.SimulationOfBus.Data;
using WPFTasks.Models.SimulationOfBus.Repositories;
using WPFTasks.ViewModels;

namespace WPFTasks.Models.SimulationOfBus.View
{
    public class BusView : BaseViewModel
    {
        private static StopDisplayRepository rep = Simulation.GetRepository<StopDisplayRepository>();
        private Point _pos;
        private SolidColorBrush _filler;
        public Point Pos
        {
            get => _pos;
            set => SetProperty(ref _pos, value);
        }
        public SolidColorBrush Filler
        {
            get => _filler;
            set => SetProperty(ref _filler, value);
        }
        public string Statistic => $"{BaseBus.Passengers.Count}/{BaseBus.MaxCapacity}\nNumber: {BaseBus.Number}";
        public Bus BaseBus { get; }


        public BusView(Bus core)
        {
            BaseBus = core ?? throw new ArgumentNullException(nameof(core));
            Filler = Simulation.Brushes[BaseBus.Number];
            BaseBus.OnUpdateToNextStopProgress += OnUpdateProgress;
            
        }

        private void OnUpdateProgress(double progress)
        {
            var currentStop = rep.GetStop(BaseBus.CurrentStop)
                              ?? throw new InvalidOperationException("Current stop not found");
            var nextStop = rep.GetStop(BaseBus.NextStop
                              ?? throw new InvalidOperationException("Next stop not found"))
                              ?? throw new InvalidOperationException("Next stop position not found");

            Pos = Interpolate(currentStop.Pos, nextStop.Pos, progress);
        }

        public static Point Interpolate(Point start, Point end, double progress)
        {
            progress = Math.Clamp(progress, 0.0, 1.0);

            double x = start.X + (end.X - start.X) * progress;
            double y = start.Y + (end.Y - start.Y) * progress;

            return new Point(x, y);
        }

    }
}
