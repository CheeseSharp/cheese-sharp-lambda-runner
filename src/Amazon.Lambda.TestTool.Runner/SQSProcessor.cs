using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Lambda.TestTool;
using Amazon.Lambda.TestTool.Runtime;
using Amazon.SQS;
using Amazon.SQS.Model;
using CheeseSharp.Lambda.TestTool.Runner.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static CheeseSharp.Lambda.TestTool.Runner.Models.SQSEvent;

namespace CheeseSharp.Lambda.TestTool.Runner
{
    public class SQSProcessor : BackgroundService
    {
        private readonly ILogger<SQSProcessor> logger;
        private readonly IAmazonSQS sqs;
        private readonly LocalLambdaOptions localLambdaOptions;
        private readonly LambdaTriggerMaps lambdaTriggerMaps;

        public SQSProcessor(
            ILogger<SQSProcessor> logger, 
            IAmazonSQS sqs, 
            LocalLambdaOptions localLambdaOptions,
            LambdaTriggerMaps lambdaTriggerMaps)
        {
            this.logger = logger;
            this.sqs = sqs;
            this.localLambdaOptions = localLambdaOptions;
            this.lambdaTriggerMaps = lambdaTriggerMaps;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    foreach (var lambdaTriggerMap in this.lambdaTriggerMaps.Maps)
                    {
                        var queueUrl = await sqs.GetQueueUrlAsync(lambdaTriggerMap.FunctionResource, stoppingToken);
                        var queueArn = await sqs.GetQueueAttributesAsync(queueUrl.QueueUrl, new List<string> { "QueueArn" });
                        var request = new ReceiveMessageRequest
                        {
                            QueueUrl = queueUrl.QueueUrl,
                            MaxNumberOfMessages = 10,
                            WaitTimeSeconds = 5
                        };

                        var result = await sqs.ReceiveMessageAsync(request);
                        if (result.Messages.Any())
                        {
                            logger.LogInformation($"Messages received {result.Messages.Count}");

                            var sqsEventJson = JsonConvert.SerializeObject(
                                this.MapToSQSEvent(result, lambdaTriggerMap.FunctionResource));

                            var responses = new List<ExecutionResponse>();

                            foreach (var triggers in lambdaTriggerMap.LambdaTriggerTargets)
                            {
                                var function = localLambdaOptions.LoadLambdaFuntion(
                               triggers.ConfigFileLocation,
                               triggers.FunctionHandler);

                                var executionRequest = new ExecutionRequest()
                                {
                                    Function = function,
                                    AWSProfile = "default",
                                    AWSRegion = sqs.Config.RegionEndpoint.SystemName,
                                    Payload = sqsEventJson
                                };

                                var response = 
                                    await localLambdaOptions.LambdaRuntime.ExecuteLambdaFunctionAsync(executionRequest);
                                responses.Add(response);

                                if (!response.IsSuccess)
                                {
                                    logger.LogError(
                                        $"Function {triggers.FunctionHandler} responded with error {response.Error}");
                                }
                            }                        

                            if(responses.All(r => r.IsSuccess))
                            {
                                var deleteItems = result.Messages
                               .Select(dm => new DeleteMessageBatchRequestEntry(dm.MessageId, dm.ReceiptHandle))
                               .ToList();

                                var deleteResult = await sqs.DeleteMessageBatchAsync(
                                                            queueUrl.QueueUrl, deleteItems, stoppingToken);

                                logger.LogInformation(
                                    $"Messages delete result Successful {deleteResult.Successful.Count} and failed {deleteResult.Failed.Count}");

                                if (deleteResult.Failed.Count > 0)
                                {
                                    deleteResult.Failed.ForEach(m => logger.LogError($"Message delete failed for {m.Id} with Code {m.Code}"));
                                }
                            }                           
                        }
                    }
                    await Task.Delay(100);
                }
                catch (Exception e)
                {
                    logger.LogError(e.ToString());
                    if(!(e.InnerException is null))
                    {
                        logger.LogError(e.InnerException.ToString());
                    }                    
                }

                logger.LogInformation($"Worker running at: {DateTimeOffset.Now}");
            }
        }

        private SQSEvent MapToSQSEvent(ReceiveMessageResponse resposne, string queueArn)
        {
            if (resposne == null)
            {
                throw new ArgumentNullException($"{nameof(resposne)} is null");
            }

            if (!resposne.Messages.Any())
            {
                throw new ArgumentException($"{nameof(resposne)} has not messages");
            }

            var sqsEvent = new SQSEvent();
            sqsEvent.Records = resposne.Messages.Select(m =>
            {
                var record = new SQSMessage()
                {
                    MessageId = m.MessageId,
                    ReceiptHandle = m.ReceiptHandle,
                    Body = m.Body,
                    Md5OfBody = m.MD5OfBody,
                    Md5OfMessageAttributes = m.MD5OfMessageAttributes,
                    EventSource = "aws:sqs",
                    EventSourceArn = queueArn,
                    Attributes = m.Attributes,
                    MessageAttributes = this.MapToMessageAttributes(m.MessageAttributes),
                };

                return record;
            }).ToList();

            return sqsEvent;
        }

        private Dictionary<string, MessageAttribute> MapToMessageAttributes(Dictionary<string, MessageAttributeValue> messageAttributes)
        {
            return messageAttributes.Select(ma => 
            {
                return new 
                {
                    Key = ma.Key,
                    Value = new MessageAttribute 
                    { 
                        StringValue = ma.Value.StringValue,
                        BinaryValue = ma.Value.BinaryValue,
                        StringListValues = ma.Value.StringListValues,
                        BinaryListValues = ma.Value.BinaryListValues,
                        DataType = ma.Value.DataType,
                    },
                };
            }).ToDictionary(d => d.Key, d => d.Value);
        }
    }
}
