using System;
using System.Threading.Tasks;
using OsuMissAnalyzer.Server;

namespace OsuMissAnalyzer.Tests
{
    public class TestLogger : ILogger
    {
        public event Action UpdateLogs;

        public void Close()
        {
        }

        public void Log(Logging type, int count)
        {
            Console.WriteLine(type.ToString());
        }

        public void LogAbsolute(Logging type, int value)
        {
            Console.WriteLine($"{type.ToString()} {value}");
        }

        public async Task LogException(Exception exception, Logger.LogLevel level)
        {
            Console.WriteLine(exception);
            await Task.CompletedTask;
        }

        public async Task WriteLine(string line, Logger.LogLevel level)
        {
            Console.WriteLine(line);
            await Task.CompletedTask;
        }
    }
}