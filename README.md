## Lambda Runner
The Lambda Runner allows a developer to run Lambda locally against a SQS queue as part of a more complex solution.
When the developer debugs the solution this runner will load the Lambda DLL and execute the Lambda based on the setting in the
function `aws-lambda-tools-test-xxx.json`. One `.json` file per Lambda entry point.
The `.json` file is a the Amazon.Lambda.TestTool with an extra setting.

    {
      "Information": [
        "This file provides default values for the deployment wizard inside Visual Studio and the AWS Lambda commands added to the .NET Core CLI.",
        "To learn more about the Lambda commands with the .NET Core CLI execute the following command at the command line in the project root directory.",
        "dotnet lambda help",
        "All the command line options for the Lambda command can be specified in this file."
      ],
      "profile": "default", ## AWS credentials profile
      "region": "",
      "configuration": "Release",
      "framework": "netcoreapp3.1",
      "function-runtime": "dotnetcore3.1",
      "function-memory-size": 256,
      "function-timeout": 30,
      "function-handler": "demo-lamdba::Demo.Lamdba.Function::FunctionHandler",
      "function-trigger-resource": "test-function-runner" ## This attribute points to NAME of resource that triggers the Lambda, in this case the SQS queue name 
    }

One file is required per Lambda entry point.

>**NOTE:** AWS Lambda only supports net5.0 via a container entry point. This application is designed for testing, for deployment you will need to packed a net5.0 Lambda into a container. 

### Setup
1. Install the version 0.11.3 version of the Lambda Tools for .Net https://github.com/aws/aws-lambda-dotnet/blob/master/Tools/LambdaTestTool/README.md
1. Create a user environment variable called `AmazonLambdaTesttool` thats points to the root directory that the tools are installed in mine looks like `C:\Users\MYUSER\.dotnet\tools\.store`
1. Rebuild the application to ensure it builds
1. From the project root run 
   i. dotnet publish -c Release -f netcoreapp3.1 -o $USERPROFILE/.dotnet/tools/.store/cheese.sharp.lambda.testtool-3.1 --self-contained true -r win-x64 -p:PublishReadyToRun=true -p:PublishSingleFile=true
   i. dotnet publish -c Release -f net5.0 -o $USERPROFILE/.dotnet/tools/.store/cheese.sharp.lambda.testtool-5.0 --self-contained true -r win-x64 -p:PublishReadyToRun=true -p:PublishSingleFile=true

### Use
The Runner works in the same way and extends the Amazon.Lambda.TestTool.

1. Set your project up to use Lambda, which will set up the `launchsettings.json`
2. Modify the `launchsettings.json` 

>**NOTE:** Changes will be reverted from time to time when the nuget packages are updated

**From**

    netcoreapp3.1
        {
          "profiles": {
            "Mock Lambda Test Tool": {
              "commandName": "Executable",
              "executablePath": "%USERPROFILE%\\.dotnet\\tools\\dotnet-lambda-test-tool-3.1.exe",
              "commandLineArgs": "--port 5050 --profile default", ## Set to the AWS credentials profile used on your developer machine 
              "workingDirectory": ".\\bin\\$(Configuration)\\netcoreapp3.1"
            }
          }
        }

    net5.0
        {
          "profiles": {
            "Mock Lambda Test Tool": {
              "commandName": "Executable",
              "executablePath": "%USERPROFILE%\\.dotnet\\tools\\dotnet-lambda-test-tool-5.0.exe",
              "commandLineArgs": "--port 5050 --profile default", ## Set to the AWS credentials profile used on your developer machine 
              "workingDirectory": ".\\bin\\$(Configuration)\\net5.0"
            }
          }
        }


**To**

    netcoreapp3.1
        {
          "profiles": {
            "Mock Lambda Test Tool": {
              "commandName": "Executable",
              "executablePath": "%USERPROFILE%\\.dotnet\\tools\\.store\\cheese.sharp.lambda.testtool-3.1\\dotnet-lambda-test-tool-3.1.exe",
              "commandLineArgs": "--port 5050 --profile default", ## Set to the AWS credentials profile used on your developer machine 
              "workingDirectory": ".\\bin\\$(Configuration)\\netcoreapp3.1"
            }
          }
        }

    net5.0
        {
          "profiles": {
            "Mock Lambda Test Tool": {
              "commandName": "Executable",
              "executablePath": "%USERPROFILE%\\.dotnet\\tools\\.store\\cheese.sharp.lambda.testtool-5.0\\dotnet-lambda-test-tool-5.0.exe",
              "commandLineArgs": "--port 5050 --profile default", ## Set to the AWS credentials profile used on your developer machine 
              "workingDirectory": ".\\bin\\$(Configuration)\\net5.0"
            }
          }
        }



 

