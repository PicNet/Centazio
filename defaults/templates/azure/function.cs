using Centazio.Core.Runner;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace {{it.NewAssemblyName}};

public class {{it.ClassName}}Azure(ILogger<{{it.ClassName}}Azure> log) {  
  private static readonly Lazy<Task<IRunnableFunction>> impl;

  static {{it.ClassName}}Azure() {    
    impl = new(async () => await new FunctionsInitialiser("{{it.Environment}}").Init<{{it.ClassFullName}}>(), LazyThreadSafetyMode.ExecutionAndPublication);
  }

  [Function(nameof({{it.ClassFullName}}))] public async Task Run([TimerTrigger("*/5 * * * * *")] TimerInfo timer) {    
    var start = DateTime.Now;
    log.LogInformation("{{it.ClassName}} running");
    try { await (await impl.Value).RunFunction(); } 
    finally { log.LogInformation($"{{it.ClassName}} completed, took {(DateTime.Now - start).TotalSeconds:N0}s"); }
  }
}