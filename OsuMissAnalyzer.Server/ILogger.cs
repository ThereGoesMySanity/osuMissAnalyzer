using System;
using System.Threading.Tasks;
using static OsuMissAnalyzer.Server.Logger;
using static OsuMissAnalyzer.Server.UnixLogger;

namespace OsuMissAnalyzer.Server
{
    public interface ILogger
    {
        public event Action UpdateLogs;
        void Close();
        Task LogException(Exception exception, LogLevel level);
        void Log(Logging type, int count);
        void LogAbsolute(Logging type, int value);
        Task WriteLine(string line, LogLevel level);
    }
    public class Logger
    {
        public enum LogLevel { NORMAL, ALERT }
        public static ILogger Instance { get; set; }
        public static async Task LogException(Exception exception, LogLevel level = LogLevel.ALERT)
        {
            await Instance.LogException(exception, level);
        }
        public static void Log(Logging type, int count = 1)
        {
            Instance?.Log(type, count);
        }
        public static void LogAbsolute(Logging type, int value)
        {
            Instance?.LogAbsolute(type, value);
        }

        public static async Task WriteLine(object o, LogLevel level = LogLevel.NORMAL)
        {
            await Instance?.WriteLine(o.ToString(), level);
        }
        public static async Task WriteLine(string line, LogLevel level = LogLevel.NORMAL)
        {
            await Instance?.WriteLine(line, level);
        }
    }
}