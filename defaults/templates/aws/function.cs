using Centazio.Core.Runner;
using Centazio.Core.Misc;
using Amazon.Lambda.Core;
using Serilog;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace {{it.NewAssemblyName}};

public class {{it.ClassName}}Handler {
  private static readonly Lazy<Task<IRunnableFunction>> impl;

  static {{it.ClassName}}Handler() {    
    impl = new(async () => await new FunctionsInitialiser("{{it.Environment}}").Init<{{it.ClassName}}>(), 
        LazyThreadSafetyMode.ExecutionAndPublication);
  }

  public async Task<string> Handle(ILambdaContext context) {
    var start = UtcDate.UtcNow;
    Log.Information("{{it.ClassName}} running");
    try { 
        await (await impl.Value).RunFunction();
        return $"{{it.ClassName}} executed successfully";
    } finally { 
        Log.Information($"{{it.ClassName}} completed, took {(UtcDate.UtcNow - start).TotalSeconds:N0}s");
    }
  }
}