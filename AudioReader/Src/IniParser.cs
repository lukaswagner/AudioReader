using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioReader
{
    class IniParser
    {
        private static Dictionary<string, Dictionary<string, string>> _entries;

        static IniParser()
        {
            _entries = new Dictionary<string, Dictionary<string, string>>();
        }

        public static void Load(string filename = "config.ini")
        {
            // filter comments and empty lines
            IEnumerable<string> lines = File.ReadLines(filename).Where((line) => !line.StartsWith("#") && !line.StartsWith(";") && line.Length > 0);

            string section = "";

            foreach(string line in lines)
            {
                // section
                if (line.StartsWith("["))
                {
                    string trim = line.TrimStart('[');
                    trim = trim.TrimEnd(']');
                    trim = trim.Trim();
                    trim = trim.ToLower();
                    if (trim.Length == 0) Console.WriteLine("Invalid section in ini will be ignored: " + line);
                    else section = trim;
                    continue;
                }

                // property
                string[] split = line.Split('=');

                if(split.Length != 2 || split[0].Length == 0 || split[1].Length == 0)
                {
                    Console.WriteLine("Invalid parameter in ini will be ignored: " + line);
                    continue;
                }

                if(section == "")
                {
                    Console.WriteLine("Cant set parameter without valid section: " + line);
                    continue;
                }

                if (!_entries.TryGetValue(section, out Dictionary<string, string> sectionEntries))
                    sectionEntries = new Dictionary<string, string>();

                sectionEntries[split[0].ToLower().Trim()] = split[1].ToLower().Trim();
                _entries[section] = sectionEntries;
            }
        }

        public static Dictionary<string, string> GetSectionParameter(string section)
        {
            if (!_entries.TryGetValue(section, out Dictionary<string, string> sectionEntries))
                sectionEntries = new Dictionary<string, string>();
            return sectionEntries;
        }
    }
}
