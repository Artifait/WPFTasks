
using System.Text;

namespace WPFTasks.Core.Models
{
    public class Logger
    {
        private string _log = string.Empty;
        public StringBuilder sb { get; set; } = new();
        public event Action<string>? OnUpdateLog;
        public event Action<string>? OnLogged;
        public string Log => _log;

        public void LogString(string str)
        {
            sb.AppendLine(str);
            _log = sb.ToString();

            OnUpdateLog?.Invoke(_log);
            OnLogged?.Invoke(str);
        }
        public void ClearLog() => sb.Clear();
    }
}
