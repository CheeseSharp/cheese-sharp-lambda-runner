using Amazon.Lambda.TestTool;
using Amazon.Lambda.TestTool.Runtime;
using CheeseSharp.Lambda.TestTool.Runner.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace CheeseSharp.Lambda.TestTool.Runner.Processors
{
    public class ProcessLambda : IProcessLambda
    {
        private readonly ILogger<ProcessLambda> logger;
        private readonly LocalLambdaOptions localLambdaOptions;

        public ProcessLambda(
           ILogger<ProcessLambda> logger,
           LocalLambdaOptions localLambdaOptions)
        {
            if (localLambdaOptions == null)
            {
                throw new ArgumentNullException(nameof(localLambdaOptions));
            }

            this.logger = logger;
            this.localLambdaOptions = localLambdaOptions;
        }

        public Task<ExecutionResponse> FindFunctionAndExec(LambdaTriggerTarget trigger) 
            => this.FindFunctionAndExec(new ExecutionRequest(), trigger);

        public async Task<ExecutionResponse> FindFunctionAndExec(ExecutionRequest request, LambdaTriggerTarget trigger)
        {
            if (trigger == null)
            {
                throw new ArgumentNullException(nameof(trigger));
            }

            logger.LogInformation($"Start Function {trigger.FunctionHandler}");

            var function = localLambdaOptions.LoadLambdaFuntion(
                               trigger.ConfigFileLocation,
                               trigger.FunctionHandler);

            request.Function = function;

            var response =
                await localLambdaOptions.LambdaRuntime.ExecuteLambdaFunctionAsync(request);


            if (!response.IsSuccess)
            {
                logger.LogError(
                    $"Function {trigger.FunctionHandler} responded with error {response.Error}");
            }

            return response;
        }
    }
}
