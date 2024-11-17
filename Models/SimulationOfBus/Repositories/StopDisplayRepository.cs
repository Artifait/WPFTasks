using System;
using System.Collections.Generic;
using WPFTasks.Models.SimulationOfBus.Data;
using WPFTasks.Models.SimulationOfBus.View;
using WPFTasks.SimulationArchitecture;

namespace WPFTasks.Models.SimulationOfBus.Repositories
{
    internal class StopDisplayRepository : Repository
    {
        private List<StopView> _stopList = [];
        public List<StopView> StopList
        {
            get => _stopList;
        }

        public StopView AddStop(StopView sd)
        {
            var stop = GetStop(sd.BaseStop.Name);

            if (stop == null)
            {
                _stopList.Add(sd);
                stop = sd;
            }

            return stop;
        }
        public void RemoveStop(string name)
        {
            var stop = GetStop(name);

            if (stop != null)
                _stopList.Remove(stop);
        }
        public StopView? GetStop(Stop stop) => GetStop(stop.Name);
        public StopView? GetStop(string name) => _stopList.Find(x => x.BaseStop.Name == name);
        public void RemoveStop(StopView stop) => RemoveStop(stop.BaseStop.Name);

        public override void OnCreate() { }
        public override void Initialize() { }
        public override void OnStart() { }
    }
}
