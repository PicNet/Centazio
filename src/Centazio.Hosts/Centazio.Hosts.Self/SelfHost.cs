using Centazio.Core;
using Centazio.Core.Ctl;
using Centazio.Core.Engine;
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

  public List<Type> GetFunctions() {
    var assemblies = AssemblyNames.Split(',').Select(ReflectionUtils.LoadAssembly).ToList();
    return assemblies.SelectMany(ass => IntegrationsAssemblyInspector.GetRequiredCentazioFunctions(ass, ParseFunctionFilters())).ToList();
  }
  
}

public class SelfHostCentazioEngineAdapter(CentazioSettings settings, List<string> environments) : CentazioEngine(environments) {
  protected override void RegisterHostSpecificServices(CentazioServicesRegistrar registrar) {
    using var notifier = new InProcessChangesNotifier();
    
    registrar.Register<IChangesNotifier>(notifier);
    registrar.Register<IFunctionRunner>(prov => {
      var inner = new FunctionRunner(prov.GetRequiredService<ICtlRepository>(), settings);
      return new FunctionRunnerWithNotificationAdapter(inner, notifier, () => {});
    });
  }
}

public class SelfHost(CancellationToken? cancel = null) {
  public async Task RunHost(CentazioSettings settings, IHostConfiguration cmdsetts, CentazioEngine centazio) {
    GlobalHostInit(cmdsetts);

    var functypes = cmdsetts.GetFunctions();
    var prov = await centazio.Init(functypes);
    
    await StartHost(settings, functypes, prov);
  }

  private static void GlobalHostInit(IHostConfiguration cmdsetts) {
    Environment.SetEnvironmentVariable("CENTAZIO_HOST", "true");
    
    Log.Logger = LogInitialiser.GetConsoleConfig(cmdsetts.GetLogLevel(), cmdsetts.GetLogFilters()).CreateLogger();
    Log.Information("\nPress 'Enter' to exit\n\n");
    
    FunctionConfigDefaults.ThrowExceptions = true;
  }

  private async Task StartHost(CentazioSettings settings, List<Type> functypes, ServiceProvider prov) {
    var (runner, notifier) = (prov.GetRequiredService<IFunctionRunner>(), prov.GetRequiredService<IChangesNotifier>());
    var functions = functypes.Select(prov.GetRequiredService).Cast<IRunnableFunction>().ToList();
    
    StartTimerBasedTriggers(settings, functions, runner);
    notifier.Init(functions);
    
    _ = notifier.Run(runner);
    
    var maxtime = Task.Delay(Timeout.Infinite, cancel ?? CancellationToken.None);
    var runnning = Task.Run(() => { Console.ReadLine(); }); // exit on 'Enter'
    await Task.WhenAny(maxtime, runnning);
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