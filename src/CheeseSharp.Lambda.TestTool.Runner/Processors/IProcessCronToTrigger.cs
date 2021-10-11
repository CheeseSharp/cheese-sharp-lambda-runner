using CheeseSharp.Lambda.TestTool.Runner.Models;
using System;
using System.Collections.Generic;

namespace CheeseSharp.Lambda.TestTool.Runner.Processors
{
    public interface IProcessCronToTrigger
    {
        IEnumerable<LambdaTriggerTarget> GetTriggers(IEnumerable<LambdaTriggerMap> maps, DateTime baseTime, TimeSpan includeInFuture);
    }
}
