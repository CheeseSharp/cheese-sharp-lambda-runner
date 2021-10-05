using Amazon.Lambda.TestTool;
using Amazon.Lambda.TestTool.Runtime;
using CheeseSharp.Lambda.TestTool.Runner.Models;
using System.IO;
using System.Linq;
using System.Text.Json;
using static Amazon.Lambda.TestTool.TestToolStartup;

namespace CheeseSharp.Lambda.TestTool.Runner
{
    public class LambdaOptionsBuilder
    {
        public LambdaTriggerMaps BuildLambdaTriggerMaps(LocalLambdaOptions localLambdaOptions)
        {
            var lambdaTriggerMapConfigFiles = localLambdaOptions.LambdaConfigFiles.Select(fi =>
            {
                if (!File.Exists(fi))
                {
                    throw new FileNotFoundException($"Lambda Trigger Map config file {fi} not found");
                }

                var config = JsonSerializer.Deserialize<LambdaTriggerMapConfigFile>(File.ReadAllText(fi).Trim(), new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                config.ConfigFileLocation = fi;
                return config;
            });

            var items = lambdaTriggerMapConfigFiles
                .GroupBy(fi => fi.FunctionTriggerResource)
                .Select(gp => new LambdaTriggerMap(gp.Key, gp.Select(i =>
                                                       new LambdaTriggerTarget(i.FunctionHandler, i.ConfigFileLocation))));

            return new LambdaTriggerMaps(items);
        }


        public LocalLambdaOptions BuildLambdaOptions(string productName, CommandLineOptions commandOptions, RunConfiguration runConfiguration)
        {
            Utils.PrintToolTitle(productName);
            var localLambdaOptions = new LocalLambdaOptions()
            {
                Port = commandOptions.Port
            };

            var lambdaAssemblyDirectory = commandOptions.Path ?? Directory.GetCurrentDirectory();

#if NETCOREAPP2_1
                var targetFramework = "netcoreapp2.1";
#elif NETCOREAPP3_1
            var targetFramework = "netcoreapp3.1";
#elif NET5_0
                var targetFramework = "net5.0";
#endif

            // Check to see if running in debug mode from this project's directory which means the test tool is being debugged.
            // To make debugging easier pick one of the test Lambda projects.
            if (lambdaAssemblyDirectory.EndsWith("Amazon.Lambda.TestTool.WebTester21"))
            {
                lambdaAssemblyDirectory = Path.Combine(lambdaAssemblyDirectory, $"../../tests/LambdaFunctions/netcore21/S3EventFunction/bin/Debug/{targetFramework}");
            }
            else if (lambdaAssemblyDirectory.EndsWith("Amazon.Lambda.TestTool.WebTester31"))
            {
                lambdaAssemblyDirectory = Path.Combine(lambdaAssemblyDirectory, $"../../tests/LambdaFunctions/netcore31/S3EventFunction/bin/Debug/{targetFramework}");
            }
            // If running in the project directory select the build directory so the deps.json file can be found.
            else if (Utils.IsProjectDirectory(lambdaAssemblyDirectory))
            {
                lambdaAssemblyDirectory = Path.Combine(lambdaAssemblyDirectory, $"bin/Debug/{targetFramework}");
            }

            localLambdaOptions.LambdaRuntime = LocalLambdaRuntime.Initialize(lambdaAssemblyDirectory);
            runConfiguration.OutputWriter.WriteLine($"Loaded local Lambda runtime from project output {lambdaAssemblyDirectory}");

            // Look for aws-lambda-tools-defaults.json or other config files.
            localLambdaOptions.LambdaConfigFiles = Utils.SearchForConfigFiles(lambdaAssemblyDirectory);

            return localLambdaOptions;
        }
    }
}
