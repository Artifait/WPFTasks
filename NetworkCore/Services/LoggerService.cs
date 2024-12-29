
using System.Text;

namespace TopNetwork.Services
{
    public class LoggerService
    {
        private readonly StringBuilder sb = new();
        public Action<string>? OnUpdateLog { get; set; }
        public string LogMsgs => sb.ToString();

        public void Log(string str)
        {
            sb.AppendLine(str);
            OnUpdateLog?.Invoke(sb.ToString());
        }
        public void ClearLog() => sb.Clear();
    }
}
