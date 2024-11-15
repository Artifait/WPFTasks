using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFTasks.Models.SimulationOfBus
{
    public class Stop
    {
        public string Name { get; set; }
        public List<Passenger> WaitingPassengers { get; set; }

        public Stop(string name)
        {
            Name = name;
            WaitingPassengers = [];
        }

        public void AddPassenger(Passenger passenger)
        {
            WaitingPassengers.Add(passenger);
        }

        public override bool Equals(object obj)
        {
            if (obj is Stop otherStop)
            {
                return Name == otherStop.Name;
            }
            return false;
        }

        public override int GetHashCode()
            => Name != null ? Name.GetHashCode() : 0;

        public static bool operator ==(Stop left, Stop right)
        {
            if (ReferenceEquals(left, null))
                return ReferenceEquals(right, null);
            return left.Equals(right);
        }

        public static bool operator !=(Stop left, Stop right)
            => !(left == right);
    }

}
