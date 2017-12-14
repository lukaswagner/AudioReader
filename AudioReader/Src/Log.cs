using System;
using System.Collections.Concurrent;
using System.Threading;

namespace AudioReader
{
    static class Log
    {
        public enum LogLevel
        {
            Verbose, Debug, Info, Warn, Error
        }

        private class LogEntry
        {
            public LogLevel LogLevel;
            public string Tag;
            public string Message;

            public LogEntry(LogLevel logLevel, string tag, string message)
            {
                LogLevel = logLevel;
                Tag = tag;
                Message = message;
            }

            public void LogToConsole()
            {
                if (LogLevel >= Log.Level)
                {
                    if (_changeConsoleColor(LogLevel, out ConsoleColor consoleColor))
                    {
                        Console.ForegroundColor = consoleColor;
                    }
                    Console.WriteLine(_getLogLevelString(LogLevel) + " " + Tag.PadRight(TagLength).Substring(0, TagLength) + " : " + Message);
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
        }

        private static BlockingCollection<LogEntry> _queue = new BlockingCollection<LogEntry>();
        private static Thread _logLoopThread;
        private static bool _runLogLoop = true;
        private static object _logLoopLock = new object();
        public static LogLevel Level = LogLevel.Info;
        public static int TagLength = 15;

        static Log()
        {
            _logLoopThread = new Thread(_logLoop);
            _logLoopThread.Start();
            AppDomain.CurrentDomain.ProcessExit += _stopLogLoop;
        }

        private static void _stopLogLoop(object sender, EventArgs e)
        {
            lock (_logLoopLock)
            {
                _runLogLoop = false;
            }
            _logLoopThread.Join();
        }

        private static void _logLoop()
        {
            while (true)
            {
                lock (_logLoopLock)
                {
                    if (!_runLogLoop)
                    {
                        return;
                    }
                }
                _queue.Take().LogToConsole();
            }
        }

        public static void Verbose(string tag, string message) =>_queue.Add(new LogEntry(LogLevel.Verbose, tag, message));
        public static void Debug(string tag, string message) => _queue.Add(new LogEntry(LogLevel.Debug, tag, message));
        public static void Info(string tag, string message) => _queue.Add(new LogEntry(LogLevel.Info, tag, message));
        public static void Warn(string tag, string message) => _queue.Add(new LogEntry(LogLevel.Warn, tag, message));
        public static void Error(string tag, string message) => _queue.Add(new LogEntry(LogLevel.Error, tag, message));
    }
}
