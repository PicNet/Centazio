using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Centazio.Hosts.Az;

namespace {{it.NewAssemblyName}};

public class {{it.ClassName}}Azure(ILogger<{{it.ClassName}}Azure> log) {
  [Function(nameof({{it.ClassName}}))] public async Task Run([TimerTrigger("{{ it.FunctionTimerCronExpr }}")] TimerInfo timer) {    
    var start = UtcDate.UtcNow;
    log.LogInformation("{{it.ClassName}} running");
    try { 
      await AzHost.RunFunction(typeof({{it.ClassName}}), [new TimerChangeTrigger("{{ it.FunctionTimerCronExpr }}")]); 
    } finally { log.LogInformation($"{{it.ClassName}} completed, took {(UtcDate.UtcNow - start).TotalSeconds:N0}s"); }
  }
}
