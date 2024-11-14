using IniParser;
using IniParser.Model;
using System.IO;
using System.Text;

namespace WPFTasks.Models
{
    public class IniFileReader
    {
        public List<string> BannedWords { get; private set; }
        public List<string> AllowedExtensions { get; private set; }
        public string ReplacementWord { get; private set; }

        public IniFileReader(string iniFilePath)
        {
            BannedWords = [];
            AllowedExtensions = [];
            ParseIniFile(iniFilePath);
        }

        private void ParseIniFile(string filePath)
        {
            var parser = new FileIniDataParser();
            IniData data;

            using (var reader = new StreamReader(filePath, Encoding.GetEncoding(1251)))
            {
                data = parser.ReadData(reader);
            }

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

            if(data.Sections.ContainsSection("Replacement"))
            {
                ReplacementWord = data["Replacement"]["word"];
            }
            else
            {
                ReplacementWord = "*******";
            }
        }
    }
}
