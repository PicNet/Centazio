using Centazio.Core;
using Centazio.Core.Ctl;
using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Centazio.Core.Settings;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Serilog.Events;
using Timer = System.Threading.Timer;

namespace Centazio.Hosts.Self;

public interface IHostConfiguration {
  public List<string> EnvironmentsList { get; }
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

// todo: see if we can use IHostInitialiser for consistency with other 'clouds'
public class SelfHost {
  
  public async Task Run(CentazioSettings settings, IHostConfiguration cmdsetts) {
    Environment.SetEnvironmentVariable("CENTAZIO_HOST", "true");
    
    Log.Logger = LogInitialiser.GetConsoleConfig(cmdsetts.GetLogLevel(), cmdsetts.GetLogFilters()).CreateLogger();
    Log.Information("\nPress 'Enter' to exit\n\n");
    
    FunctionConfigDefaults.ThrowExceptions = true;
    var assemblies = cmdsetts.AssemblyNames.Split(',').Select(ReflectionUtils.LoadAssembly).ToList();
    var functypes = assemblies.SelectMany(ass => IntegrationsAssemblyInspector.GetCentazioFunctions(ass, cmdsetts.ParseFunctionFilters())).ToList();
    var registrar = new CentazioServicesRegistrar(new ServiceCollection());
    
    await new FunctionsInitialiser(cmdsetts.EnvironmentsList.AddIfNotExists(GetType().Name.ToLower()), registrar)
        .Init(functypes);
    
    using var notifier = new InProcessChangesNotifier();
    var inner = new FunctionRunner(registrar.Get<ICtlRepository>(), settings);
    var runner = new FunctionRunnerWithNotificationAdapter(inner, notifier, () => {});
    
    var functions = functypes.Select(t => registrar.Get<IRunnableFunction>(t)).ToList();
    StartTimerBasedTriggers(settings, functions, runner);
    notifier.Init(functions);
    _ = notifier.Run(runner);
    
    await Task.Run(() => { Console.ReadLine(); }); // exit on 'Enter' 
  }

  private void StartTimerBasedTriggers(CentazioSettings settings, List<IRunnableFunction> functions, IFunctionRunner runner) {
    functions
        .GroupBy(f => f.GetFunctionPollCronExpression(settings.Defaults))
        .ForEach(g => _ = new FunctionTimerGroup(g.Key, g.ToList(), RunFunctionsInGroupAndResetTimer));
    
    // ReSharper disable once AsyncVoidMethod
    async void RunFunctionsInGroupAndResetTimer(FunctionTimerGroup g) {
      await g.Timer.DisposeAsync();
      await g.Functions.Select(async f => await runner.RunFunction(f, [new TimerChangeTrigger(g.Cron.Expression)])).Synchronous();
      g.Timer = new Timer(_ => RunFunctionsInGroupAndResetTimer(g), null, g.Delay(UtcDate.UtcNow), Timeout.InfiniteTimeSpan);
    }
  }
  
  public class FunctionTimerGroup {
    public ValidCron Cron { get; }
    public List<IRunnableFunction> Functions { get; }
    public Timer Timer { get; set; }  

    public FunctionTimerGroup(ValidString cron, List<IRunnableFunction> functions, Action<FunctionTimerGroup> rungroup) {
      (Cron, Functions) = (new(cron.Value), functions);
      Timer = new Timer(_ => rungroup(this), null, Delay(UtcDate.UtcNow), Timeout.InfiniteTimeSpan);
    }
    
    public TimeSpan Delay(DateTime utcnow) => Cron.Value.GetNextOccurrence(utcnow) - utcnow ?? throw new Exception();
  }
}