using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AudioReader
{
    class IniParser
    {
        private static Dictionary<string, Dictionary<string, string>> _entries;
        private static string _filename;

        static IniParser()
        {
            _entries = new Dictionary<string, Dictionary<string, string>>();
        }

        public static void Load(string filename = "config.ini")
        {
            _filename = filename;

            // filter comments and empty lines
            IEnumerable<string> lines = File.ReadLines(filename).Where((line) => !line.StartsWith("#") && !line.StartsWith(";") && line.Length > 0);

            string section = "";

            foreach (string line in lines)
            {
                // section
                if (_parseSection(line, out string newSection))
                {
                    section = newSection;
                    continue;
                }

                // property
                string[] split = line.Split('=');

                if (split.Length != 2 || split[0].Length == 0 || split[1].Length == 0)
                {
                    Console.WriteLine("Invalid property in ini will be ignored: " + line);
                    continue;
                }

                if (section == "")
                {
                    Console.WriteLine("Cant set property without valid section: " + line);
                    continue;
                }

                if (!_entries.TryGetValue(section, out Dictionary<string, string> sectionEntries))
                    sectionEntries = new Dictionary<string, string>();

                sectionEntries[split[0].ToLower().Trim()] = split[1].ToLower().Trim();
                _entries[section] = sectionEntries;
            }
        }

        private static bool _parseSection(string line, out string section)
        {
            section = "";

            if (!line.StartsWith("["))
                return false;

            string trim = line.TrimStart('[');
            trim = trim.TrimEnd(']');
            trim = trim.Trim();
            trim = trim.ToLower();

            if (trim.Length == 0)
            {
                Console.WriteLine("Invalid section in ini will be ignored: " + line);
                return false;
            }

            else section = trim;
            return true;
        }

        private static bool _parseProperty(string line, out string property, out string value)
        {
            property = "";
            value = "";

            string[] split = line.Split('=');

            if (split.Length != 2 || split[0].Length == 0 || split[1].Length == 0)
            {
                Console.WriteLine("Invalid property in ini will be ignored: " + line);
                return false;
            }

            property = split[0];
            value = split[1];
            return true;
        }

        public static Dictionary<string, string> GetSectionProperties(string section)
        {
            if (!_entries.TryGetValue(section, out Dictionary<string, string> sectionEntries))
                sectionEntries = new Dictionary<string, string>();
            return sectionEntries;
        }

        public static void Write(string section, string parameter, object value)
        {
            string value_string = value.ToString();

            IEnumerable<string> lines = File.ReadLines(_filename);

            if (_entries.TryGetValue(section, out Dictionary<string, string> sectionEntries))
                if (sectionEntries.TryGetValue(parameter, out string oldValue))
                {
                    sectionEntries[parameter] = value_string;
                    string sec = "";
                    foreach(string line in lines)
                    {
                        if(line.)
                    }
                }
                else
                    _entries[section] = new Dictionary<string, string>() { { parameter, value_string } };

            Save();
        }

        public static void Save()
        {
            string result = "";

            foreach (KeyValuePair<string, Dictionary<string, string>> section in _entries)
            {
                result += "[" + section.Key + "]" + Environment.NewLine;

                foreach (KeyValuePair<string, string> property in section.Value)
                {
                    result += property.Key + "=" + property.Value + Environment.NewLine;
                }
            }

            File.WriteAllText(_filename, result);
        }
    }
}
