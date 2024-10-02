using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace GameHook.Domain
{
    /*
    public class ExecutionStopwatch
    {
        private ILogger Logger { get; }
        private Stopwatch Stopwatch { get; }
        private Guid Id { get; }
        private string Name { get; }

        public ExecutionStopwatch(ILogger logger, string name)
        {
            Logger = logger;
            Name = name;

            Id = Guid.NewGuid();
            Stopwatch = Stopwatch.StartNew();
        }

        public void Stop()
        {
            Stopwatch.Stop();

            Logger.LogInformation($"ExecutionStopwatch: {Id} finished {Name} in {Stopwatch.Elapsed.TotalSeconds} seconds.");
        }
    }

    public class DisposableExecutionStopwatch : ExecutionStopwatch, IDisposable
    {
        public DisposableExecutionStopwatch(ILogger logger, string name) : base(logger, name)
        {
        }

        public void Dispose()
        {
            Stop();
        }
    }
    */
}
