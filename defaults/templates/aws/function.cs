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

public class {{it.ClassName}}Handler : IAwsFunctionHandler {

  public async Task<string> Handle(ILambdaContext context) {
    var start = UtcDate.UtcNow;
    Log.Information("{{it.ClassName}} running");
    try {
      
      // TODO process sqs message
      //    if the message for this lambda function
      //    if this is triggered for the timer
      //    otherwise skip
      
      await AwsHost.RunFunction([new TimerChangeTrigger("{{ it.FunctionTimerCronExpr }}")]);
      return $"{{it.ClassName}} executed successfully";
    } finally { Log.Information($"{{it.ClassName}} completed, took {(UtcDate.UtcNow - start).TotalSeconds:N0}s"); }
  }
}