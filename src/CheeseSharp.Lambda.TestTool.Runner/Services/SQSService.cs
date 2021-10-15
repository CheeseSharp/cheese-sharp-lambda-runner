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
using CheeseSharp.Lambda.TestTool.Runner.Processors;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using static CheeseSharp.Lambda.TestTool.Runner.Models.SQSEvent;

namespace CheeseSharp.Lambda.TestTool.Runner.Services
{
    public class SQSService : BackgroundService
    {
        private readonly ILogger<SQSService> logger;
        private readonly IAmazonSQS sqs;
        private readonly IProcessLambda processLambda;
        private readonly LambdaTriggerMaps lambdaTriggerMaps;

        public SQSService(
            ILogger<SQSService> logger, 
            IAmazonSQS sqs,
            IProcessLambda processLambda,
            LambdaTriggerMaps lambdaTriggerMaps)
        {
            this.logger = logger;
            this.sqs = sqs;
            this.processLambda = processLambda;
            this.lambdaTriggerMaps = lambdaTriggerMaps;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var maps = this.lambdaTriggerMaps.FindByTriggerType(TriggerType.sqs);

                    logger.LogInformation($"Trigger Maps Found for {TriggerType.sqs} Found: {string.Join(",", maps.Select(m => m.FunctionResource.FunctionResource))}");

                    foreach (var lambdaTriggerMap in maps)
                    {
                        var queueUrl = await sqs.GetQueueUrlAsync(lambdaTriggerMap.FunctionResource.FunctionResource, stoppingToken);
                        var queueArn = await sqs.GetQueueAttributesAsync(queueUrl.QueueUrl, new List<string> { "QueueArn" });
                        var request = new ReceiveMessageRequest
                        {
                            QueueUrl = queueUrl.QueueUrl,
                            MaxNumberOfMessages = 10,
                            WaitTimeSeconds = 5
                        };

                        var result = await sqs.ReceiveMessageAsync(request, stoppingToken);
                        if (result.Messages.Any())
                        {
                            logger.LogInformation($"Messages received {result.Messages.Count}");

                            var sqsEventJson = JsonConvert.SerializeObject(
                                this.MapToSQSEvent(result, lambdaTriggerMap.FunctionResource.FunctionResource));

                            var executionRequest = new ExecutionRequest()
                            {
                                AWSProfile = "default",
                                AWSRegion = sqs.Config.RegionEndpoint.SystemName,
                                Payload = sqsEventJson
                            };

                            var responses = new List<ExecutionResponse>();

                            foreach (var trigger in lambdaTriggerMap.LambdaTriggerTargets)
                            {   
                                responses.Add(await this.processLambda.FindFunctionAndExec(executionRequest, trigger));
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
                }
                catch (Exception e)
                {
                    logger.LogError(e.ToString());
                    if(!(e.InnerException is null))
                    {
                        logger.LogError(e.InnerException.ToString());
                    }                    
                }
                finally
                {
                    await Task.Delay(1000);
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
