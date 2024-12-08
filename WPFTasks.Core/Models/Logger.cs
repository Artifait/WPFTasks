
using System.Text;

namespace WPFTasks.Core.Models
{
    public class Logger
    {
        public StringBuilder sb { get; set; } = new();
        public Action<string>? OnUpdateLog { get; set; }

        public void Log(string str)
        {
            sb.AppendLine(str);
            OnUpdateLog?.Invoke(sb.ToString());
        }
        public void ClearLog() => sb.Clear();
    }
}
