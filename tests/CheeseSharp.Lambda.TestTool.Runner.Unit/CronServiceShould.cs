using Amazon.Lambda.TestTool;
using Amazon.Lambda.TestTool.Runtime;
using CheeseSharp.Lambda.TestTool.Runner.Models;
using CheeseSharp.Lambda.TestTool.Runner.Processors;
using CheeseSharp.Lambda.TestTool.Runner.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace CheeseSharp.Lambda.TestTool.Runner.Unit
{
    public class CronServiceShould
    {
        private readonly ITestOutputHelper output;
        private readonly ServiceProvider serviceProvider;

        public CronServiceShould(ITestOutputHelper output)
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

            services.AddSingleton<IProcessTime>(c => new DefaultProcessTime(2));
            services.AddSingleton<IProcessCronToTrigger, ProcessCronToTrigger>();

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
            moqProcessLambda.Setup(m => m.FindFunctionAndExec(It.IsAny<LambdaTriggerTarget>()))
                .ReturnsAsync(new ExecutionResponse() { Response = HttpStatusCode.OK.ToString() });

            var timeevents = new Queue<DateTime>();
            timeevents.Enqueue(new DateTime(2021, 12, 1, 10, 5, 0));
            timeevents.Enqueue(new DateTime(2021, 12, 1, 10, 6, 0));
            var fakeProcessTime = new FakeProcessTime(timeevents, 2);

            var sut = new CronService(
                fakeProcessTime,
                this.serviceProvider.GetService<ILogger<CronService>>(),
                moqProcessLambda.Object,
                this.serviceProvider.GetService<IProcessCronToTrigger>(),
                this.serviceProvider.GetService<LambdaTriggerMaps>());

            var token = new CancellationTokenSource();
            token.CancelAfter(4000);

            await sut.StartAsync(token.Token);
            await Task.Delay(100);

            moqProcessLambda.Verify(v => v.FindFunctionAndExec(It.IsAny<LambdaTriggerTarget>()));
            await sut.StopAsync(token.Token);
        }

        [Fact]
        public async Task ProcessAndNotCallFindFunctionAndExec()
        {
            var moqProcessLambda = new Mock<IProcessLambda>();
            moqProcessLambda.Setup(m => m.FindFunctionAndExec(It.IsAny<LambdaTriggerTarget>()))
                .ReturnsAsync(new ExecutionResponse() { Response = HttpStatusCode.OK.ToString() });

            var timeevents = new Queue<DateTime>();
            timeevents.Enqueue(new DateTime(2021, 12, 1, 10, 6, 0));
            timeevents.Enqueue(new DateTime(2021, 12, 1, 10, 7, 0));
            timeevents.Enqueue(new DateTime(2021, 12, 1, 10, 8, 0));
            var fakeProcessTime = new FakeProcessTime(timeevents, 2);

            var sut = new CronService(
                fakeProcessTime,
                this.serviceProvider.GetService<ILogger<CronService>>(),
                moqProcessLambda.Object,
                this.serviceProvider.GetService<IProcessCronToTrigger>(),
                this.serviceProvider.GetService<LambdaTriggerMaps>());

            var token = new CancellationTokenSource();
            token.CancelAfter(4000);

            await sut.StartAsync(token.Token);
            await Task.Delay(100);

            Exception outputException = null;
            try
            {
                moqProcessLambda.Verify(v => v.FindFunctionAndExec(It.IsAny<LambdaTriggerTarget>()));
            }
            catch (Exception ex)
            {
                outputException = ex;
            }

            outputException.Should().NotBeNull();
            outputException.Message.Contains("No invocations performed");

            await sut.StopAsync(token.Token);
        }

        private class FakeProcessTime : IProcessTime
        {
            private Queue<DateTime> dateTimes;
            private TimeSpan loopTime;

            public FakeProcessTime(Queue<DateTime> dateTimes, int looptimeseconds)
            {
                this.dateTimes = dateTimes;
                this.loopTime = TimeSpan.FromSeconds(looptimeseconds);
            }

            public TimeSpan GetCurrentLoopTimer() => loopTime;

            public DateTime GetCurrentUtcTime() => this.dateTimes.Dequeue();
        }
    }
}
