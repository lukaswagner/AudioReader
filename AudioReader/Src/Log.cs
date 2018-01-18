using System;
using System.Collections.Concurrent;
using System.Threading;

namespace AudioReader
{
    static class Log
    {
        public enum LogLevel
        {
            Verbose, Debug, Info, Warn, Error, Off
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
                if (_changeConsoleColor(LogLevel, out ConsoleColor consoleColor))
                {
                    Console.ForegroundColor = consoleColor;
                }
                Console.WriteLine(_getLogLevelString(LogLevel) + " " + Tag.PadRight(TagLength).Substring(0, TagLength) + " : " + Message);
                Console.ResetColor();
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
        private static Thread _logLoopThread = new Thread(_logLoop);
        private static bool _runLogLoop = true;
        private static object _logLoopLock = new object();
        private static LogLevel _level = LogLevel.Off;
        public static int TagLength = 15;

        private static void _exitHandler(object sender, EventArgs e) => Disable();

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

        private static void _add(LogLevel logLevel, string tag, string message)
        {
            if (logLevel >= _level)
                _queue.Add(new LogEntry(logLevel, tag, message));
        }

        public static void Enable(LogLevel logLevel)
        {
            _level = logLevel;
            if(!_logLoopThread.IsAlive)
            {
                _logLoopThread.Start();
                AppDomain.CurrentDomain.ProcessExit += _exitHandler;
            }
        }

        public static void Disable()
        {
            _level = LogLevel.Off;
            lock (_logLoopLock)
            {
                _runLogLoop = false;
            }
            _logLoopThread.Join();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static void Verbose(string tag, string message) => _add(LogLevel.Verbose, tag, message);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static void Debug(string tag, string message) => _add(LogLevel.Debug, tag, message);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static void Info(string tag, string message) => _add(LogLevel.Info, tag, message);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static void Warn(string tag, string message) => _add(LogLevel.Warn, tag, message);
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        public static void Error(string tag, string message) => _add(LogLevel.Error, tag, message);
    }
}
