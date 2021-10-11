using Amazon;
using Amazon.Lambda.TestTool;
using Amazon.Lambda.TestTool.Runtime;
using Amazon.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using CheeseSharp.Lambda.TestTool.Runner.Models;
using CheeseSharp.Lambda.TestTool.Runner.Processors;
using CheeseSharp.Lambda.TestTool.Runner.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace CheeseSharp.Lambda.TestTool.Runner.Unit
{
    public class SQSServiceshould
    {
        private readonly ITestOutputHelper output;
        private readonly ServiceProvider serviceProvider;

        public SQSServiceshould(ITestOutputHelper output)
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
        public async Task Process()
        {
            var moqClientConfig = new Mock<IClientConfig>();
            moqClientConfig.SetupGet(m => m.RegionEndpoint)
                .Returns(RegionEndpoint.EUWest3);

            var moqSqs = new Mock<IAmazonSQS>();
            moqSqs.Setup(m => m.GetQueueUrlAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetQueueUrlResponse() { QueueUrl = "http://localhost:9090" });

            moqSqs.Setup(m => m.ReceiveMessageAsync(It.IsAny<ReceiveMessageRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(this.GetFakeM2essagesResponse());

            moqSqs.Setup(m => m.DeleteMessageBatchAsync(
                            It.IsAny<string>(), 
                            It.IsAny<List<DeleteMessageBatchRequestEntry>>(),
                            It.IsAny<CancellationToken>()))
                .ReturnsAsync(new DeleteMessageBatchResponse() 
                { 
                    Failed = new List<BatchResultErrorEntry>(), 
                    Successful = new List<DeleteMessageBatchResultEntry>()
                    {
                        new DeleteMessageBatchResultEntry () { Id = "Dummy Id" }
                    }
                });

            moqSqs.SetupGet(m => m.Config)
                .Returns(moqClientConfig.Object);

            var moqProcessLambda = new Mock<IProcessLambda>();
            moqProcessLambda.Setup(m => m.FindFunctionAndExec(It.IsAny<ExecutionRequest>(), It.IsAny<LambdaTriggerTarget>()))
                .ReturnsAsync(new ExecutionResponse());

            var sut = new SQSService(
                this.serviceProvider.GetService<ILogger<SQSService>>(),
                moqSqs.Object,
                moqProcessLambda.Object,
                this.serviceProvider.GetService<LambdaTriggerMaps>());

            var token = new CancellationTokenSource();
            token.CancelAfter(4000);

            await sut.StartAsync(token.Token);
            await Task.Delay(100);

            moqProcessLambda.Verify(v => v.FindFunctionAndExec(It.IsAny<ExecutionRequest>(), It.IsAny<LambdaTriggerTarget>()));
            moqSqs.Verify(v => v.DeleteMessageBatchAsync(
                            It.IsAny<string>(),
                            It.IsAny<List<DeleteMessageBatchRequestEntry>>(),
                            It.IsAny<CancellationToken>()));

            await sut.StopAsync(token.Token);
        }

        private ReceiveMessageResponse GetFakeM2essagesResponse()
        {
            var item = new ReceiveMessageResponse();
            item.Messages = new List<Message>();
            item.Messages.Add(
                new Message 
                    { 
                        Body = "Stuff", 
                        MessageId = "00000000-0000-0000-0000-000000000000", 
                        ReceiptHandle="TestHandler"
                    });
            return item;
        }
    }
}
