using System.Collections.Generic;
using System.IO;

namespace CheeseSharp.Lambda.TestTool.Runner.Models
{
    public class SQSEvent
    {
        public SQSEvent()
        { 
        }

        public List<SQSMessage> Records { get; set; }

        public class MessageAttribute
        {
            public MessageAttribute()
            {
            }

            public string StringValue { get; set; }

            public MemoryStream BinaryValue { get; set; }

            public List<string> StringListValues { get; set; }

            public List<MemoryStream> BinaryListValues { get; set; }

            public string DataType { get; set; }
        }

        public class SQSMessage
        {
            public SQSMessage()
            {
            }

            public string MessageId { get; set; }

            public string ReceiptHandle { get; set; }

            public string Body { get; set; }

            public string Md5OfBody { get; set; }

            public string Md5OfMessageAttributes { get; set; }

            public string EventSourceArn { get; set; }

            public string EventSource { get; set; }

            public string AwsRegion { get; set; }

            public Dictionary<string, string> Attributes { get; set; }

            public Dictionary<string, MessageAttribute> MessageAttributes { get; set; }
        }
    }
}
