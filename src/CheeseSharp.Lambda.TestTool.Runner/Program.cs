using System;
using System.IO;
using Amazon;
using Amazon.Lambda.TestTool;
using Amazon.SQS;
using CheeseSharp.Lambda.TestTool.Runner.Processors;
using CheeseSharp.Lambda.TestTool.Runner.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CheeseSharp.Lambda.TestTool.Runner
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    var builder = new ConfigurationBuilder()
                            .SetBasePath(Directory.GetCurrentDirectory())
                            //.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                            //.AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", optional: false, reloadOnChange: true)
                            .AddEnvironmentVariables()
                            .Build();
                })
                .ConfigureServices((hostingContext, services) =>
                {
                    var commandOptions = CommandLineOptions.Parse(args);
                    var options = hostingContext.Configuration.GetAWSOptions();

                    if (!string.IsNullOrEmpty(commandOptions.AWSRegion))
                    {
                        options.Region = RegionEndpoint.GetBySystemName(commandOptions.AWSRegion);
                    }

                    if (!string.IsNullOrEmpty(commandOptions.AWSProfile))
                    {
                        options.Profile = commandOptions.AWSProfile;
                    }

                    services.AddDefaultAWSOptions(options);
                    services.AddAWSService<IAmazonSQS>();
                    services.AddSingleton(c =>
                    {
                        var builder = new LambdaOptionsBuilder();
                        return builder.BuildLambdaOptions(
                            "AWS .NET Mock Lambda Test Tool",
                            commandOptions,
                            new TestToolStartup.RunConfiguration());
                    });

                    services.AddSingleton(c =>
                    {
                        var builder = new LambdaOptionsBuilder();
                        return builder.BuildLambdaTriggerMaps(c.GetService<LocalLambdaOptions>());
                    });

                    services.AddSingleton<IProcessLambda, ProcessLambda>();
                    services.AddSingleton<IProcessCronToTrigger, ProcessCronToTrigger>();
                    services.AddSingleton<IProcessTime>(c => new DefaultProcessTime(15));
                    services.AddHostedService<SimpleService>();
                    services.AddHostedService<CronService>();
                    services.AddHostedService<SQSService>();
                });
    }
}
