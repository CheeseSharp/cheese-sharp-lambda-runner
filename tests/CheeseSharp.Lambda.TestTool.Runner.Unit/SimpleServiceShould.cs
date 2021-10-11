using Amazon.Lambda.TestTool;
using Amazon.Lambda.TestTool.Runtime;
using CheeseSharp.Lambda.TestTool.Runner.Models;
using CheeseSharp.Lambda.TestTool.Runner.Processors;
using CheeseSharp.Lambda.TestTool.Runner.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace CheeseSharp.Lambda.TestTool.Runner.Unit
{
    public class SimpleServiceShould
    {
        private readonly ITestOutputHelper output;
        private readonly ServiceProvider serviceProvider;

        public SimpleServiceShould(ITestOutputHelper output)
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

        [Fact]
        public async Task ProcessAndCallFindFunctionAndExec()
        {
            var moqProcessLambda = new Mock<IProcessLambda>();
            moqProcessLambda.Setup(m => m.FindFunctionAndExec(It.IsAny<ExecutionRequest>(), It.IsAny<LambdaTriggerTarget>()))
                .ReturnsAsync(new ExecutionResponse() { Response = HttpStatusCode.OK.ToString() });

            var sut = new SimpleService(
                this.serviceProvider.GetService<ILogger<SimpleService>>(),
                moqProcessLambda.Object,
                this.serviceProvider.GetService<LambdaTriggerMaps>());

            var token = new CancellationTokenSource();
            token.CancelAfter(4000);

            await sut.StartAsync(token.Token);
            await Task.Delay(1000);

            moqProcessLambda.Verify(v => v.FindFunctionAndExec(
                It.Is<ExecutionRequest>(p => p.Payload.Equals(JsonConvert.SerializeObject("DataToBeSent"))), It.IsAny<LambdaTriggerTarget>()));
            await sut.StopAsync(token.Token);
        }
    }
}
