using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TSW2LM
{
    class Log
    {
        private static Dictionary<string, LogLevel> LogPaths;

        public static LogLevel ConsoleLevel = LogLevel.INFO;

        public static bool AddLogFile(string path, LogLevel level)
        {
            if (LogPaths.ContainsKey(path))
            {
                LogPaths[path] = level;
                return false;
            }
            try
            {
                File.OpenWrite(path).Close();
                LogPaths.Add(path, level);
                return true;
            }
            catch (Exception e)
            {
                throw new IOException($"There was an error accessing {path} in an attempt to add it as a level {level} log file.");
            }
        }

        public static void AddLogMessage(string message, string? stack = "-", LogLevel? level = LogLevel.INFO)
        {
            string Timestamp = DateTime.Now.ToString("MMddTHH:mm:ss.fff");
            string LogLine = $"[{level.ToString()}] {Timestamp} {stack} | {message}\n";
            if (ConsoleLevel >= level) Console.Write(LogLine);
            foreach (KeyValuePair<string, LogLevel> p in LogPaths.Where(pair => pair.Value >= level))
            {
                File.AppendAllText(p.Key, LogLine);
            }
        }

        public enum LogLevel
        {
            ERROR = 1,
            WARNING = 2,
            INFO = 3,
            DEBUG = 4
        }

    }
}
