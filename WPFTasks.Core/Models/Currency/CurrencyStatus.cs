
using TopNetwork.Core.Defaults;

namespace WPFTasks.Core.Models.Currency
{
    public class CurrencyStatus : DefaultServerStatus
    {
        public readonly int MaxActiveConnection = 10;

        public override string GetStatusSummary()
        {
            return base.GetStatusSummary() +
                $"\nЗанято {ActiveConnections}/{MaxActiveConnection} соединений.";
        }
    }
}