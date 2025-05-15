using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Microsoft.Extensions.DependencyInjection;
using Amazon.Lambda.Core;
using Amazon.Lambda.RuntimeSupport;
using Serilog;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace {{it.FunctionNamespace}}.Aws;

public class {{it.ClassName}}Handler {
  private static readonly CentazioServicesRegistrar registrar = new(new ServiceCollection());
  private static readonly Lazy<Task<IRunnableFunction>> impl;

  static {{it.ClassName}}Handler() {
    registrar.Register<IFunctionRunner, FunctionRunner>();
    impl = new(async () => (await new FunctionsInitialiser({{it.Environments}}, registrar)
        .Init([typeof({{it.ClassName}})])).Single(), LazyThreadSafetyMode.ExecutionAndPublication);
  }

  public async Task<string> Handle(ILambdaContext context) {
    var start = UtcDate.UtcNow;
    Log.Information("{{it.ClassName}} running");
    try { 
      var (function, runner) = (await impl.Value, registrar.ServiceProvider.GetRequiredService<IFunctionRunner>());
      await runner.RunFunction(function, [new TimerChangeTrigger("{{ it.FunctionTimerCronExpr }}")]);
      return $"{{it.ClassName}} executed successfully";
    } finally { Log.Information($"{{it.ClassName}} completed, took {(UtcDate.UtcNow - start).TotalSeconds:N0}s"); }
  }
}