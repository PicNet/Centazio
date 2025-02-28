using Centazio.Core.Runner;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace {{NewAssemblyName}};

public class {{ClassName}}Azure(ILogger<{{ClassName}}Azure> log) {  
  private static readonly Lazy<Task<IRunnableFunction>> impl;

  static {{ClassName}}Azure() {    
    impl = new(async () => await new FunctionsInitialiser("{{Environment}}").Init<{{ClassFullName}}>(), LazyThreadSafetyMode.ExecutionAndPublication);
  }

  [Function(nameof({{ClassFullName}}))] public async Task Run([TimerTrigger("*/5 * * * * *")] TimerInfo timer) {    
    var start = DateTime.Now;
    log.LogInformation("{{ClassName}} running");
    try { await (await impl.Value).RunFunction(); } 
    finally { log.LogInformation($"{{ClassName}} completed, took {(DateTime.Now - start).TotalSeconds:N0}s"); }
  }
}