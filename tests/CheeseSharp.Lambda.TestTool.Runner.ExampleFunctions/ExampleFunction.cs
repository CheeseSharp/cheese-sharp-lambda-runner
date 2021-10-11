using Amazon.Lambda.Core;
using System;
using System.Threading.Tasks;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]
namespace CheeseSharp.Lambda.TestTool.Runner.ExampleFunctions
{
    public class ExampleFunction
    {
        public ExampleFunction()
        {
        }

        public Task SimpleFunctionHandler(string input, ILambdaContext context)
        {
            return Task.CompletedTask;
        }
    }
}
