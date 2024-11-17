using WPFTasks.Models.SimulationOfBus.Data;
using WPFTasks.SimulationArchitecture;

namespace WPFTasks.Models.SimulationOfBus.Repositories
{
    public class StopRepository : Repository
    {
        private List<Stop> _stopList = [];
        public List<Stop> StopList
        {
            get => _stopList;
        }

        public Stop AddStop(string name)
        {
            var stop = GetStop(name);

            if (stop == null)
            {
                stop = new Stop(name);
                _stopList.Add(stop);
            }

            return stop;
        }
        public void RemoveStop(string name)
        {
            var stop = GetStop(name);

            if (stop != null)
                _stopList.Remove(stop);
        }

        public Stop? GetStop(string name) => _stopList.Find(x => x.Name == name);
        public void RemoveStop(Stop stop) => RemoveStop(stop.Name);

        public override void OnCreate() { }
        public override void Initialize() { }
        public override void OnStart() { }
    }
}
