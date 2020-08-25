#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace TSW2LM
{
    class Log
    {
        private static Dictionary<string, LogLevel> LogPaths = new Dictionary<string, LogLevel>();

        /// <summary>The highest LogLevel shown on the Console</summary>
        public static LogLevel ConsoleLevel = LogLevel.INFO;

        /// <summary>  Adds a log file to be written to</summary>
        /// <param name="path">The path of the new log file, relative or absolute.</param>
        /// <param name="level">The highest LogLevel that will be written to this file.</param>
        /// <returns>true, if the file was added successfully, false, if the file was already added, only the level has been changed</returns>
        /// <exception cref="IOException">There was an error accessing {path} in an attempt to add it as a level {level} log file.</exception>
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
            catch (Exception)
            {
                throw new IOException($"There was an error accessing {path} in an attempt to add it as a level {level} log file.");
            }
        }

        /// <summary>  Logs a message in the format "[&lt;LEVEL&gt;] &lt;Time&gt; &lt;stack&gt; | &lt;message&gt;"</summary>
        /// <param name="message">The log message.</param>
        /// <param name="stack">A simple string representation of the call stack</param>
        /// <param name="level">The LogLevel of this message, it will only be logged to each file that has a LogLevel at or above that of the message</param>
        public static void AddLogMessage(string message, string? stack = "-", LogLevel? level = LogLevel.INFO)
        {
            string Timestamp = DateTime.Now.ToString("MMddTHH:mm:ss.fff");
            string LogLine = $"[{level.ToString()}] {Timestamp} {stack} | {message}\n";
            if (ConsoleLevel >= level)
            {
                Trace.Write(LogLine);
                Console.Write(LogLine);
            }
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
