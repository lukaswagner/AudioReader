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

        public static LogLevel Level = LogLevel.Info;
        public static int TagLength = 15;

        private static void _log(LogLevel logLevel, string tag, string message)
        {
            if(logLevel >= Level)
            {
                Console.WriteLine(_getLogLevelString(logLevel) + " " + tag.PadRight(TagLength).Substring(0, TagLength) + " : " + message);
            }
        }

        private static string _getLogLevelString(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Verbose:
                    return "[Vrb]";
                case LogLevel.Debug:
                    return "[Dbg]";
                case LogLevel.Info:
                    return "[Inf]";
                case LogLevel.Warn:
                    return "[Wrn]";
                case LogLevel.Error:
                    return "[Err]";
                default:
                    return "[N/A]";
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
