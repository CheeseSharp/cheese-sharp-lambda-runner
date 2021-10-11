using System;
using System.Collections.Generic;
using System.Linq;

using Xunit;
using Xunit.Abstractions;
using CheeseSharp.Lambda.TestTool.Runner.Models;
using FluentAssertions;
using CheeseSharp.Lambda.TestTool.Runner.Processors;
using Microsoft.Extensions.DependencyInjection;
using Amazon.Lambda.TestTool;
using Microsoft.Extensions.Logging;
using Amazon.Lambda.TestTool.Runtime;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CheeseSharp.Lambda.TestTool.Runner.Unit
{
    public class ProcessLambdaShould
    {
        private readonly ITestOutputHelper output;
        private readonly ServiceProvider serviceProvider;

        public ProcessLambdaShould(ITestOutputHelper output)
        {
            this.output = output;
            var services = new ServiceCollection();
            services.AddSingleton(c =>
            {
                var builder = new LambdaOptionsBuilder();
                return builder.BuildLambdaOptions(
                    "AWS .NET Mock Lambda Test Tool",
                    CommandLineOptions.Parse(new string[0]),
                    new TestToolStartup.RunConfiguration());
            });

            services.AddSingleton(c =>
            {
                var builder = new LambdaOptionsBuilder();
                return builder.BuildLambdaTriggerMaps(c.GetService<LocalLambdaOptions>());
            });
            services.AddLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Information);
                logging.AddConsole();
                logging.AddDebug();
            });

            this.serviceProvider = services.BuildServiceProvider();
        }

        [Theory]
        [InlineData("2008-11-01T19:35:00.0000000Z", 1)]
        [InlineData("2008-11-01T19:40:00.0000000Z", 2)]
        [InlineData("2008-11-01T19:00:00.0000000Z", 3)]
        [InlineData("2008-11-02T00:00:00.0000000Z", 4)]
        [InlineData("2008-11-01T02:00:00.0000000Z", 4)]
        [InlineData("2008-11-02T00:00:00.0200000Z", 0)]
        public void CronOutputTriggers(string inputDate, int resultNo)
        {
            this.output.WriteLine($"Date: {inputDate} Expected Results {resultNo}");

            var date = DateTime.Parse(inputDate);
            var data = this.GetCrons()
                .Select(x => new LambdaTriggerMap(
                    new LambdaTriggerMapFunctionResource(x, TriggerType.cron),
                    new List<LambdaTriggerTarget> { new LambdaTriggerTarget("function", "location") }));
            var sut = new ProcessCronToTrigger();
            var result = sut.GetTriggers(data, date, TimeSpan.FromSeconds(3));
            result.Count().Should().Be(resultNo);
        }

        [Fact]
        public async Task ProcessASimpleFunction()
        {
            var lambdaTriggerMaps = this.serviceProvider.GetService<LambdaTriggerMaps>();
            var maps = lambdaTriggerMaps.FindByTriggerType(TriggerType.simple);

            var sut = new ProcessLambda(
                this.serviceProvider.GetService<ILogger<ProcessLambda>>(),
                this.serviceProvider.GetService<LocalLambdaOptions>());
            var map = maps.First();
            var result = await sut.FindFunctionAndExec(
                new ExecutionRequest 
                { 
                    Payload = JsonConvert.SerializeObject(map.FunctionResource.FunctionResource), 
                }, 
                map.LambdaTriggerTargets.First());

            result.Should().NotBeNull();

            if (!result.IsSuccess)
            {
                this.output.WriteLine($"Logs: {result.Logs}");
                this.output.WriteLine($"Error: {result.Error}");
            }
            result.IsSuccess.Should().BeTrue();
        }

        private IEnumerable<string> GetCrons()
        {
            var items = new List<string>
            {
                "0 */5 * * * *",
                "0 0 2 * * *",
                "0 0 0 * * *",
                "0 */10 * * * *",
                "0 0 * * * *",
            };

            return items;
        }
    }
}
