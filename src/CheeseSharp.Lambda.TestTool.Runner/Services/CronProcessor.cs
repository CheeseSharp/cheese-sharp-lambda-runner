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
using NCrontab;

namespace CheeseSharp.Lambda.TestTool.Runner.Services
{
    public class CronService : BackgroundService
    {
        private readonly IProcessTime processTime;
        private readonly ILogger<CronService> logger;
        private readonly IProcessLambda processLambda;
        private readonly IProcessCronToTrigger processCronToTrigger;
        private readonly LambdaTriggerMaps lambdaTriggerMaps;

        public CronService(
            IProcessTime processTime,
            ILogger<CronService> logger,
            IProcessLambda processLambda,
            IProcessCronToTrigger processCronToTrigger,
            LambdaTriggerMaps lambdaTriggerMaps)
        {
            this.processTime = processTime;
            this.logger = logger;
            this.processLambda = processLambda;
            this.processCronToTrigger = processCronToTrigger;
            this.lambdaTriggerMaps = lambdaTriggerMaps;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var baseTime = this.processTime.GetCurrentUtcTime();
            var loopTimespan = this.processTime.GetCurrentLoopTimer();
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var maps = this.lambdaTriggerMaps.FindByTriggerType(TriggerType.cron);

                    logger.LogInformation($"Trigger Maps Found for {TriggerType.cron} Found: {string.Join(",", maps.Select(m => m.FunctionResource.FunctionResource))}");

                    var tasks = this.processCronToTrigger.GetTriggers(maps, baseTime, loopTimespan)
                        .Select(t => this.processLambda.FindFunctionAndExec(t));

                    var response = await Task.WhenAll(tasks);
                    if (!response.All(r => r.IsSuccess))
                    {
                        response.Where(r => !r.IsSuccess).ToList().ForEach(r =>
                            this.logger.LogError($"Error in {r.GetType().Name} with response {r.Response}."));
                    }

                    baseTime = baseTime.Add(loopTimespan);
                    var currentTime = this.processTime.GetCurrentUtcTime();
                    while (baseTime >= currentTime)
                    {
                        currentTime = this.processTime.GetCurrentUtcTime();
                    }
                    await Task.Delay(100);
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
}
