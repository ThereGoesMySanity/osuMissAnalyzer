using System;
using System.Threading;
using System.Threading.Tasks;
using OsuMissAnalyzer.Server.Logging;

namespace OsuMissAnalyzer.Tests
{
    public class TestLogger : IDataLogger
    {
        public event Action UpdateLogs;

        public void Log(DataPoint type, int count)
        {
            Console.WriteLine(type.ToString());
        }

        public void Log(DataPoint type)
        {
            Console.WriteLine(type.ToString());
        }

        public void LogAbsolute(DataPoint type, int value)
        {
            Console.WriteLine($"{type.ToString()} {value}");
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}