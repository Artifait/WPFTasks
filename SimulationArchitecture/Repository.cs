using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFTasks.SimulationArchitecture
{
    public abstract class Repository
    {
        public abstract void OnCreate();
        public abstract void Initialize();
        public abstract void OnStart();

        public virtual void OnDispose() { }
    }
}

