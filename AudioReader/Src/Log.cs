using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioReader
{
    static class Log
    {
        public enum LogLevel
        {
            Verbose, Debug, Info, Warn, Error
        }

        public static LogLevel _logLevel = LogLevel.Info;

        private static void _log(LogLevel logLevel, string tag, string message)
        {
            if(logLevel >= _logLevel)
            {
                Console.WriteLine(message);
            }
        }

        public static void V(string tag, string message)
        {
            _log(LogLevel.Verbose, tag, message);
        }

        public static void D(string tag, string message)
        {
            _log(LogLevel.Debug, tag, message);
        }

        public static void I(string tag, string message)
        {
            _log(LogLevel.Info, tag, message);
        }

        public static void W(string tag, string message)
        {
            _log(LogLevel.Warn, tag, message);
        }

        public static void E(string tag, string message)
        {
            _log(LogLevel.Error, tag, message);
        }
    }
}
