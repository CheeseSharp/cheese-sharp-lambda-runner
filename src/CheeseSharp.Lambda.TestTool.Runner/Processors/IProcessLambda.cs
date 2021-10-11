using Amazon.Lambda.TestTool.Runtime;
using CheeseSharp.Lambda.TestTool.Runner.Models;
using System.Threading.Tasks;

namespace CheeseSharp.Lambda.TestTool.Runner.Processors
{
    public interface IProcessLambda
    {
        Task<ExecutionResponse> FindFunctionAndExec(LambdaTriggerTarget trigger);

        Task<ExecutionResponse> FindFunctionAndExec(ExecutionRequest request, LambdaTriggerTarget trigger);
    }
}
