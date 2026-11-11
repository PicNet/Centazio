using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Centazio.Hosts.Aws;
using Microsoft.Extensions.DependencyInjection;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Serilog;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace {{it.FunctionNamespace}}.Aws;

public class {{it.ClassName}}Handler(AwsHost host) : IAwsFunctionHandler {

  public async Task<string> Handle(ILambdaContext context) {
    var start = TimeProvider.System.GetTimestamp();
    Log.Information("{{it.ClassName}} running");
    try {
      await host.RunFunction([new TimerChangeTrigger("{{ it.FunctionTimerCronExpr }}")]);
      return $"{{it.ClassName}} executed successfully";
    } finally { Log.Information($"{{it.ClassName}} completed, took {TimeProvider.System.GetElapsedTime(start).TotalSeconds:N0}s"); }
  }
}