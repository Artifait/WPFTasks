using System.Windows;
using System.Windows.Media;
using WPFTasks.Models.SimulationOfBus.Data;
using WPFTasks.Models.SimulationOfBus.Repositories;
using WPFTasks.ViewModels;

namespace WPFTasks.Models.SimulationOfBus.View
{
    public class BusView : BaseViewModel
    {
        private static readonly object locker = new();
        private static StopDisplayRepository rep = Simulation.GetRepository<StopDisplayRepository>();

        private Point _pos;
        private SolidColorBrush _filler;
        private string _statistic;

        public Point Pos
        {
            get
            {
                lock (locker)
                {
                    return _pos;
                }
            }
            set
            {
                lock (locker)
                {
                    SetProperty(ref _pos, value);
                }
            }
        }

        public SolidColorBrush Filler
        {
            get
            {
                lock (locker)
                {
                    return _filler;
                }
            }
            set
            {
                lock (locker)
                {
                    SetProperty(ref _filler, value);
                }
            }
        }

        public string Statistic
        {
            get => _statistic;
            set
            {
                if (_statistic != value)
                {
                    _statistic = value;
                    OnPropertyChanged(nameof(Statistic));
                }
            }
        }
        public Bus BaseBus { get; }

        public BusView(Bus core)
        {
            BaseBus = core ?? throw new ArgumentNullException(nameof(core));
            Filler = Simulation.Brushes[BaseBus.Number];
            BaseBus.OnUpdateToNextStopProgress += OnUpdateProgress;
            BaseBus.OnBusCameToStop += OnBusCameToStop;
        }

        private void OnBusCameToStop(Stop obj)
        {
            Statistic = $"{BaseBus.CurrentPassengerCount}/{BaseBus.MaxCapacity}\nNumber: {BaseBus.Number}";
        }

        private void OnUpdateProgress(double progress)
        {
            lock (locker)
            {
                var currentStop = rep.GetStop(BaseBus.CurrentStop)
                                  ?? throw new InvalidOperationException("Current stop not found");

                var nextStop = rep.GetStop(BaseBus.NextStop
                                  ?? throw new InvalidOperationException("Next stop not found"))
                                  ?? throw new InvalidOperationException("Next stop position not found");

                Pos = Interpolate(currentStop.Pos, nextStop.Pos, progress);
            }
        }

        public static Point Interpolate(Point start, Point end, double progress)
        {
            progress = Math.Clamp(progress, 0.0, 1.0);
            return new Point(
                start.X + (end.X - start.X) * progress,
                start.Y + (end.Y - start.Y) * progress
            );
        }
    }
}
