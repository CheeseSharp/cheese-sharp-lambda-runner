using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Xunit;
using Amazon.Lambda.TestUtilities;
using Xunit.Abstractions;
using CheeseSharp.Lambda.TestTool.Runner.Models;
using Amazon.Lambda.TestTool;
using FluentAssertions;

namespace CheeseSharp.Lambda.TestTool.Runner.Unit
{
    public class LambdaOptionsBuilderShould
    {
        private readonly ITestOutputHelper output;

        public LambdaOptionsBuilderShould(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void OutputALambdaOptions()
        {
            var sut = new LambdaOptionsBuilder();
            var result = sut.BuildLambdaOptions("Test Product", CommandLineOptions.Parse(new string[0]), new TestToolStartup.RunConfiguration());
            result.LambdaConfigFiles.Count().Should().BeGreaterThan(0);
        }

        [Fact]
        public void OutputALambdaTriggerMapsSimpleViaFindByTriggerType()
        {
            var sut = new LambdaOptionsBuilder();
            var lambdaOptions = sut.BuildLambdaOptions(
                "Test Product", CommandLineOptions.Parse(new string[0]), new TestToolStartup.RunConfiguration());

            var result = sut.BuildLambdaTriggerMaps(lambdaOptions);
            result.Maps.Count().Should().BeGreaterThan(0);

            var iut = result.FindByTriggerType(TriggerType.simple).First();
            iut.FunctionResource.FunctionResource.Should().Be("DataToBeSent");
            iut.FunctionResource.FunctionResorceType.Should().BeOneOf(TriggerType.simple);
        }

        [Fact]
        public void OutputALambdaTriggerMapsSqsViaFindByTriggerType()
        {
            var sut = new LambdaOptionsBuilder();
            var lambdaOptions = sut.BuildLambdaOptions(
                "Test Product", CommandLineOptions.Parse(new string[0]), new TestToolStartup.RunConfiguration());

            var result = sut.BuildLambdaTriggerMaps(lambdaOptions);
            result.Maps.Count().Should().BeGreaterThan(0);

            var iut = result.FindByTriggerType(TriggerType.sqs).First();
            iut.FunctionResource.FunctionResource.Should().Be("sqs-test-function-runner");
            iut.FunctionResource.FunctionResorceType.Should().BeOneOf(TriggerType.sqs);
        }

        [Fact]
        public void OutputALambdaTriggerMapsCronViaFindByTriggerType()
        {
            var sut = new LambdaOptionsBuilder();
            var lambdaOptions = sut.BuildLambdaOptions(
                "Test Product", CommandLineOptions.Parse(new string[0]), new TestToolStartup.RunConfiguration());

            var result = sut.BuildLambdaTriggerMaps(lambdaOptions);
            result.Maps.Count().Should().BeGreaterThan(0);

            var iut = result.FindByTriggerType(TriggerType.cron).First();
            iut.FunctionResource.FunctionResource.Should().Be("0 */5 * * * *");
            iut.FunctionResource.FunctionResorceType.Should().BeOneOf(TriggerType.cron);
        }
    }
}
