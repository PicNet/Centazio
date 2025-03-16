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
    var runner = BuildFunctionRunner(new SelfHostChangesNotifier(pubsub.Writer), settings, registrar.ServiceProvider);
    await using var timer = StartHost(settings, functions, runner);
    _ = DoDynamicTriggers(functions, pubsub.Reader, runner);
    
    await Task.Run(() => { Console.ReadLine(); }); // exit on 'Enter'
    pubsub.Writer.Complete();
  }

  private Task DoDynamicTriggers(List<IRunnableFunction> functions, ChannelReader<OpChangeTriggerKey> reader, IFunctionRunner runner) {
    var triggermap = new Dictionary<OpChangeTriggerKey, List<IRunnableFunction>>();
    
    functions.ForEach(func => func.Triggers().ForEach(key => {
      if (!triggermap.ContainsKey(key)) triggermap[key] = [];
      triggermap[key].Add(func);
    }));
    
    return Task.Run(async () => {
      while (await reader.WaitToReadAsync()) {
        while (reader.TryRead(out var key)) {
          if (!triggermap.TryGetValue(key, out var pubs)) return;
          await pubs
              .Select(async f => {
                DataFlowLogger.Log($"Func-To-Func Trigger[{key.Object}]", key.Stage, f.GetType().Name, [key.Object]);
                return await runner.RunFunction(f);
              })
              .Synchronous();
        }
      }
    });
  }

  private static IFunctionRunner BuildFunctionRunner(SelfHostChangesNotifier notifier, CentazioSettings settings, ServiceProvider prov) => 
      new FunctionRunner(notifier, prov.GetRequiredService<ICtlRepository>(), settings);

  private Timer StartHost(CentazioSettings settings, List<IRunnableFunction> functions, IFunctionRunner runner) {
    return new Timer(RunFunctions, null, TimeSpan.Zero, TimeSpan.FromSeconds(functions.GetMinimumPollSeconds(settings.Defaults)));
    
    // ReSharper disable once AsyncVoidMethod
    async void RunFunctions(object? state) => await functions
        .Select(async f => await runner.RunFunction(f))
        .Synchronous();
  }
}

public class SelfHostChangesNotifier(ChannelWriter<OpChangeTriggerKey> onfire) : IChangesNotifier {
  
  public async Task Notify(LifecycleStage stage, List<ObjectName> objs) => 
      await Task.WhenAll(objs.Select(async obj => await onfire.WriteAsync(new (obj, stage))));

}
