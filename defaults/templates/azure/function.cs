using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace {{it.NewAssemblyName}};

public class {{it.ClassName}}Azure(ILogger<{{it.ClassName}}Azure> log) {
  private static readonly CentazioServicesRegistrar registrar = new(new ServiceCollection());
  private static readonly Lazy<Task<IRunnableFunction>> impl;

  static {{it.ClassName}}Azure() {    
    impl = new(async () => await new FunctionsInitialiser("{{it.Environment}}", registrar)
        .Init<{{it.ClassName}}>(), LazyThreadSafetyMode.ExecutionAndPublication);
  }

  [Function(nameof({{it.ClassName}}))] public async Task Run([TimerTrigger("*/5 * * * * *")] TimerInfo timer) {    
    var start = UtcDate.UtcNow;
    log.LogInformation("{{it.ClassName}} running");
    try { 
      var (function, runner) = (await impl.Value, registrar.ServiceProvider.GetRequiredService<IFunctionRunner>());
      await runner.RunFunction(function); 
    } finally { log.LogInformation($"{{it.ClassName}} completed, took {(UtcDate.UtcNow - start).TotalSeconds:N0}s"); }
  }
}
