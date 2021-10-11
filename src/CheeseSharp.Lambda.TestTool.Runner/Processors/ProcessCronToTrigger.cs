using CheeseSharp.Lambda.TestTool.Runner.Models;
using NCrontab;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CheeseSharp.Lambda.TestTool.Runner.Processors
{
    public class ProcessCronToTrigger : IProcessCronToTrigger
    {
        public IEnumerable<LambdaTriggerTarget> GetTriggers(IEnumerable<LambdaTriggerMap> maps, DateTime baseTime, TimeSpan includeInFuture)
        {
            var offsetTime = baseTime.AddMilliseconds(-1);
            var cronTriggers = maps
                       .Select(t =>
                       {
                           var cron = CrontabSchedule.Parse(
                                t.FunctionResource.FunctionResource,
                                new CrontabSchedule.ParseOptions { IncludingSeconds = true });
                           return Tuple.Create(cron.GetNextOccurrence(offsetTime), t);
                       });

            return cronTriggers.Where(ct => ct.Item1 <= offsetTime.AddSeconds(includeInFuture.TotalSeconds))
                                .SelectMany(t => t.Item2.LambdaTriggerTargets);
        }
    }
}
