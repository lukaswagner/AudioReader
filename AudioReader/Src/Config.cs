using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace AudioReader
{
    class Config
    {
        private static XmlDocument _doc;

        static Config()
        {
            _doc = new XmlDocument();
            _doc.Load("Config/config.xml");
        }

        public static bool Get<T>(string property, out T value)
        {
            value = default(T);
            string valueString = "";

            XmlNode node = _doc.SelectSingleNode("/config/" + property);

            if (node == null)
            {
                Log.Warn("Config", "Tried to read property which doesn't exist: " + property);
                return false;
            }

            valueString = node.InnerText;

            try
            {
                value = (T)Convert.ChangeType(valueString, typeof(T));
                return true;
            }
            catch (Exception)
            {
                Log.Warn("Config", "Could not convert property " + property + " with value " + valueString + " to type " + typeof(T).Name + ".");
                return false;
            }
        }

        public static T GetDefault<T>(string property, T defaultValue)
        {
            if (Get(property, out T outValue))
                return outValue;
            else
                return defaultValue;
        }

        public static bool Set(string property, object value)
        {
            string value_string = value.ToString();

            XmlNode node = _traveseOrBuildHierarchy("/config/" + property);

            node.InnerText = value_string;

            try
            {
                File.WriteAllText("Config/config.xml", Beautify(_doc));
            }
            catch (Exception e)
            {
                Log.Error("Config", "Could not set property " + property + ":\n" + e.Message);
                return false;
            }

            return true;
        }

        private static XmlNode _traveseOrBuildHierarchy(string path)
        {
            string[] split = path.Trim('/').Split('/');

            XmlNode parent = _doc as XmlNode;

            foreach(string node in split)
            {
                XmlNode current = parent.SelectSingleNode(node);
                if (current == null)
                    current = parent.AppendChild(_doc.CreateElement(node));

                parent = current;
            }

            return parent;
        }

        private static string Beautify(XmlDocument doc)
        {
            StringBuilder stringBuilder = new StringBuilder();
            XmlWriterSettings settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "    ",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace
            };
            using (XmlWriter writer = XmlWriter.Create(stringBuilder, settings))
            {
                doc.Save(writer);
            }
            return stringBuilder.ToString() + Environment.NewLine;
        }
    }
}
