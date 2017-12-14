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
                if(_changeConsoleColor(logLevel, out ConsoleColor consoleColor))
                {
                    Console.ForegroundColor = consoleColor;
                }
                Console.WriteLine(_getLogLevelString(logLevel) + " " + tag.PadRight(TagLength).Substring(0, TagLength) + " : " + message);
                Console.ResetColor();
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

        private static bool _changeConsoleColor(LogLevel logLevel, out ConsoleColor consoleColor)
        {
            consoleColor = ConsoleColor.White;
            switch (logLevel)
            {
                case LogLevel.Verbose:
                    return false;
                case LogLevel.Debug:
                    return false;
                case LogLevel.Info:
                    return false;
                case LogLevel.Warn:
                    consoleColor = ConsoleColor.Yellow;
                    return true;
                case LogLevel.Error:
                    consoleColor = ConsoleColor.Red;
                    return true;
                default:
                    return false;
            }
        }

        public static void Verbose(string tag, string message)
        {
            _log(LogLevel.Verbose, tag, message);
        }

        public static void Debug(string tag, string message)
        {
            _log(LogLevel.Debug, tag, message);
        }

        public static void Info(string tag, string message)
        {
            _log(LogLevel.Info, tag, message);
        }

        public static void Warn(string tag, string message)
        {
            _log(LogLevel.Warn, tag, message);
        }

        public static void Error(string tag, string message)
        {
            _log(LogLevel.Error, tag, message);
        }
    }
}
