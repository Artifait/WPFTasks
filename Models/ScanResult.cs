using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WPFTasks.Models
{
    public class ScanResult
    {
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public int ReplacementCount { get; set; }
        public Dictionary<string, int> WordOccurrences { get; set; } = new Dictionary<string, int>();
    }
}
