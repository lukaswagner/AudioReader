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
            _doc.Load("config.xml");
        }

        public static bool Get(string property, out string value)
        {
            value = "";

            XmlNode node = _doc.SelectSingleNode("/config/" + property);

            if (node == null) return false;

            value = node.InnerText;
            return true;
        }

        public static bool Set(string property, object value)
        {
            string value_string = value.ToString();

            XmlNode node = _traveseOrBuildHierarchy("/config/" + property);

            node.InnerText = value_string;

            try
            {
                File.WriteAllText("config.xml", Beautify(_doc));
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
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
