
using WPFTasks.Models.SimulationOfBus;

namespace WPFTasks.ViewModels
{
    public class StopDisplay
    {
        public Stop BaseStop { get; }
        public double X { get; set; }
        public double Y { get; set; }

        public StopDisplay(Stop stop, double x, double y)
        {
            BaseStop = stop;
            X = x;
            Y = y;
        }
    }
}
