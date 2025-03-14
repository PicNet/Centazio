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

// todo: add unit tests to CentazioHost
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
  
  // todo: make ObjectName, LifecycleStage a record
  private readonly Dictionary<(ObjectName, LifecycleStage), List<IRunnableFunction>> TriggerMap = [];
  private readonly Channel<(ObjectName, LifecycleStage)> pubsub = Channel.CreateUnbounded<(ObjectName, LifecycleStage)>();
  
  public async Task Run(IHostConfiguration cmdsetts) {
    Log.Logger = LogInitialiser.GetConsoleConfig(cmdsetts.GetLogLevel(), cmdsetts.GetLogFilters()).CreateLogger();
    
    FunctionConfigDefaults.ThrowExceptions = true;
    var assemblies = cmdsetts.AssemblyNames.Split(',').Select(ReflectionUtils.LoadAssembly).ToList();
    var functypes = assemblies.SelectMany(ass => IntegrationsAssemblyInspector.GetCentazioFunctions(ass, cmdsetts.ParseFunctionFilters())).ToList();
    var registrar = new CentazioServicesRegistrar(new ServiceCollection());
    var functions = await new FunctionsInitialiser(cmdsetts.Env, registrar).Init(functypes);
    
    var notif = new SelfHostChangesNotifier(pubsub.Writer);
    var runner = BuildFunctionRunner(notif, registrar.ServiceProvider);
    
    RegisterDynamicTriggers(functions);
    
    await using var timer = StartHost(functions, runner);
    
    var reader = pubsub.Reader;
    await Task.Run(async () => {
      while (await reader.WaitToReadAsync()) {
        while (reader.TryRead(out var key)) {
          if (!TriggerMap.TryGetValue((key.Item1, key.Item2), out var pubs)) return;
          await pubs
            .Select(async f => await runner.RunFunction(f))
            .Synchronous();
        }
      }
    });
    
    await DisplayInstructions();
    pubsub.Writer.Complete();
  }

  private void RegisterDynamicTriggers(List<IRunnableFunction> functions) => 
      functions.ForEach(func => func.Triggers().ForEach(key => {
        if (!TriggerMap.ContainsKey(key)) TriggerMap[key] = [];
        TriggerMap[key].Add(func);
      }));

  private static IFunctionRunner BuildFunctionRunner(SelfHostChangesNotifier notifier, ServiceProvider prov) => 
      new FunctionRunner(notifier, prov.GetRequiredService<ICtlRepository>(), prov.GetRequiredService<CentazioSettings>());

  private Timer StartHost(List<IRunnableFunction> functions, IFunctionRunner runner) {
    return new Timer(RunFunctions, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    // ReSharper disable once AsyncVoidMethod
    async void RunFunctions(object? state) => await functions
        .Select(async f => await runner.RunFunction(f))
        .Synchronous();
  }

  private Task DisplayInstructions() {
    return Task.Run(() => {
      Console.WriteLine("\nPress 'Enter' to exit\n\n");
      Console.ReadLine();
    });
  }

}

public class SelfHostChangesNotifier(ChannelWriter<(ObjectName, LifecycleStage)> onfire) : IChangesNotifier {
  
  public async Task Notify(LifecycleStage stage, List<ObjectName> objs) => 
      await Task.WhenAll(objs.Select(async obj => await onfire.WriteAsync((obj, stage))));

}
