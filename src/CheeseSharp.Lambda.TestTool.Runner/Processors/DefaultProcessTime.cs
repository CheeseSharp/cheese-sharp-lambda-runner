using System;

namespace CheeseSharp.Lambda.TestTool.Runner.Processors
{
    public class DefaultProcessTime : IProcessTime
    {
        private readonly int loopSeconds;

        public DefaultProcessTime(int loopSeconds)
        {
            this.loopSeconds = loopSeconds;
        }

        public TimeSpan GetCurrentLoopTimer() => TimeSpan.FromSeconds(this.loopSeconds);

        public DateTime GetCurrentUtcTime() => DateTime.UtcNow;
    }
}
