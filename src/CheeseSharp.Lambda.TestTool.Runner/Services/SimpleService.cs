using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.TestTool;
using Amazon.Lambda.TestTool.Runtime;
using CheeseSharp.Lambda.TestTool.Runner.Models;
using CheeseSharp.Lambda.TestTool.Runner.Processors;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace CheeseSharp.Lambda.TestTool.Runner.Services
{
    public class SimpleService : BackgroundService
    {
        private readonly ILogger<SimpleService> logger;
        private readonly IProcessLambda processLambda;
        private readonly LambdaTriggerMaps lambdaTriggerMaps;

        public SimpleService(
            ILogger<SimpleService> logger,
            IProcessLambda processLambda,
            LambdaTriggerMaps lambdaTriggerMaps)
        {
            this.logger = logger;
            this.processLambda = processLambda;
            this.lambdaTriggerMaps = lambdaTriggerMaps;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var maps = this.lambdaTriggerMaps.FindByTriggerType(TriggerType.simple);

                logger.LogInformation($"Trigger Maps Found for {TriggerType.simple} Found: {string.Join(",", maps.Select(m => m.FunctionResource.FunctionResource))}");

                var tasks = maps.Select(m => new
                        {
                            Request = new ExecutionRequest
                            {
                                Payload = string.IsNullOrEmpty(m.FunctionResource.FunctionResource)
                                        ? null : JsonConvert.SerializeObject(m.FunctionResource.FunctionResource),
                            },
                            Triggers = m.LambdaTriggerTargets
                        })
                        .SelectMany(d => d.Triggers.Select(t => this.processLambda.FindFunctionAndExec(d.Request, t)));

                var response = await Task.WhenAll(tasks);
                if (!response.All(r => r.IsSuccess))
                {
                    response.Where(r => !r.IsSuccess).ToList().ForEach(r =>
                        this.logger.LogError($"Error in {r.GetType().Name} with response {r.Response}."));
                }
            }
            catch (Exception e)
            {
                logger.LogError(e.ToString());
                if (!(e.InnerException is null))
                {
                    logger.LogError(e.InnerException.ToString());
                }
            }

            logger.LogInformation($"Worker running at: {DateTimeOffset.Now}");
        }
    }
}
