using System.Threading.Channels;
using Centazio.Core;
using Centazio.Core.Ctl;
using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Centazio.Core.Settings;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Timer = System.Threading.Timer;

namespace Centazio.Host;

public interface IHostConfiguration {
  public string Env { get; }
  public string AssemblyNames { get; }
  public string FunctionFilter { get; }
  public bool Quiet { get; }
  public bool FlowsOnly { get; }
  
  public List<string> ParseFunctionFilters() => FunctionFilter.Split(',', ';', '|', ' ').Select(f => f.Trim()).Where(f => !String.IsNullOrEmpty(f)).ToList();
  
  public LogEventLevel GetLogLevel() => Quiet ? LogEventLevel.Warning : LogEventLevel.Debug;
  
  public List<string>? GetLogFilters() {
    if (!FlowsOnly) return null;
    return [DataFlowLogger.PREFIX];
  }

}

public class CentazioHost {
  
  public async Task Run(IHostConfiguration cmdsetts) {
    Log.Logger = LogInitialiser.GetConsoleConfig(cmdsetts.GetLogLevel(), cmdsetts.GetLogFilters()).CreateLogger();
    
    Console.WriteLine("\nPress 'Enter' to exit\n\n");
    
    FunctionConfigDefaults.ThrowExceptions = true;
    var assemblies = cmdsetts.AssemblyNames.Split(',').Select(ReflectionUtils.LoadAssembly).ToList();
    var functypes = assemblies.SelectMany(ass => IntegrationsAssemblyInspector.GetCentazioFunctions(ass, cmdsetts.ParseFunctionFilters())).ToList();
    var registrar = new CentazioServicesRegistrar(new ServiceCollection());
    var functions = await new FunctionsInitialiser([cmdsetts.Env, nameof(CentazioHost).ToLower()], registrar).Init(functypes);
    var pubsub = Channel.CreateUnbounded<OpChangeTriggerKey>();
    var settings = registrar.ServiceProvider.GetRequiredService<CentazioSettings>();
    var notifier = new InProcessChangesNotifier(functions);
    var runner = new FunctionRunner(notifier, registrar.ServiceProvider.GetRequiredService<ICtlRepository>(), settings);
    
    StartTimerBasedTriggers(settings, functions, runner);
    _ = notifier.InitDynamicTriggers(runner);
    
    await Task.Run(() => { Console.ReadLine(); }); // exit on 'Enter'
    pubsub.Writer.Complete();
  }

  private void StartTimerBasedTriggers(CentazioSettings settings, List<IRunnableFunction> functions, IFunctionRunner runner) {
    functions
        .GroupBy(f => f.GetFunctionPollCronExpression(settings.Defaults))
        .ForEach(g => _ = new FunctionTimerGroup(g.Key, g.ToList(), RunFunctionsInGroupAndResetTimer));
    
    // ReSharper disable once AsyncVoidMethod
    async void RunFunctionsInGroupAndResetTimer(FunctionTimerGroup g) {
      await g.Timer.DisposeAsync();
      await g.Functions.Select(async f => await runner.RunFunction(f)).Synchronous();
      g.Timer = new Timer(_ => RunFunctionsInGroupAndResetTimer(g), null, g.Delay(UtcDate.UtcNow), Timeout.InfiniteTimeSpan);
    }
  }
  
  public class FunctionTimerGroup {
    public ValidCron Cron { get; }
    public List<IRunnableFunction> Functions { get; }
    public Timer Timer { get; set; }  

    public FunctionTimerGroup(ValidCron cron, List<IRunnableFunction> functions, Action<FunctionTimerGroup> rungroup) {
      (Cron, Functions) = (cron, functions);
      Timer = new Timer(_ => rungroup(this), null, Delay(UtcDate.UtcNow), Timeout.InfiniteTimeSpan);
    }
    
    public TimeSpan Delay(DateTime utcnow) => Cron.Value.GetNextOccurrence(utcnow) - utcnow ?? throw new Exception();
  }
}