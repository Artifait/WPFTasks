using IniParser;
using IniParser.Model;
using System.Collections.Generic;

namespace WPFTasks.Models
{
    public class IniFileReader
    {
        public List<string> BannedWords { get; private set; }
        public List<string> AllowedExtensions { get; private set; }

        public IniFileReader(string iniFilePath)
        {
            BannedWords = new List<string>();
            AllowedExtensions = new List<string>();
            ParseIniFile(iniFilePath);
        }

        private void ParseIniFile(string filePath)
        {
            var parser = new FileIniDataParser();
            IniData data = parser.ReadFile(filePath);

            if (data.Sections.ContainsSection("BannedWords"))
            {
                var bannedWords = data["BannedWords"]["words"];
                BannedWords.AddRange(bannedWords.Split(','));
            }

            if (data.Sections.ContainsSection("Extensions"))
            {
                var extensions = data["Extensions"]["list"];
                AllowedExtensions.AddRange(extensions.Split(',')); 
            }
        }
    }

}
