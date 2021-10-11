using Amazon.Lambda.TestTool.Runtime;
using CheeseSharp.Lambda.TestTool.Runner.Models;
using System;
using System.Threading.Tasks;

namespace CheeseSharp.Lambda.TestTool.Runner.Processors
{
    public interface IProcessTime
    {
        DateTime GetCurrentUtcTime();

        TimeSpan GetCurrentLoopTimer();
    }
}
