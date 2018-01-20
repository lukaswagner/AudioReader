using System;
using System.Globalization;
using System.IO;
using System.Security;
using System.Text;
using System.Xml;

namespace AudioReader
{
    internal static class Config
    {
        private static XmlDocument _doc = new XmlDocument();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
        static Config()
        {
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
                value = (T)Convert.ChangeType(valueString, typeof(T), CultureInfo.InvariantCulture);
                return true;
            }
            catch (Exception ex)
            {
                if (ex is InvalidCastException
                    || ex is FormatException
                    || ex is OverflowException
                    || ex is ArgumentNullException)
                {
                    Log.Warn("Config", "Could not convert property " + property + " with value " + valueString + " to type " + typeof(T).Name + ".");
                    return false;
                }

                throw;
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
            catch (Exception ex)
            {
                if (ex is ArgumentException
                    || ex is ArgumentNullException
                    || ex is PathTooLongException
                    || ex is DirectoryNotFoundException
                    || ex is IOException
                    || ex is UnauthorizedAccessException
                    || ex is NotSupportedException
                    || ex is SecurityException)
                {
                    Log.Error("Config", "Could not set property " + property + ":\n" + ex.Message);
                    return false;
                }

                throw;
            }

            return true;
        }

        private static XmlNode _traveseOrBuildHierarchy(string path)
        {
            string[] split = path.Trim('/').Split('/');

            XmlNode parent = _doc as XmlNode;

            foreach (string node in split)
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
