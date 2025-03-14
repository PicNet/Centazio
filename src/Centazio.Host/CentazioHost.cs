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
    
    FunctionConfigDefaults.ThrowExceptions = true;
    var assemblies = cmdsetts.AssemblyNames.Split(',').Select(ReflectionUtils.LoadAssembly).ToList();
    var functypes = assemblies.SelectMany(ass => IntegrationsAssemblyInspector.GetCentazioFunctions(ass, cmdsetts.ParseFunctionFilters())).ToList();
    var registrar = new CentazioServicesRegistrar(new ServiceCollection());
    var functions = await new FunctionsInitialiser(cmdsetts.Env, registrar).Init(functypes);
    
    await using var timer = StartHost(functions, BuildFunctionRunner(registrar.ServiceProvider));
    DisplayInstructions();
  }

  private static IFunctionRunner BuildFunctionRunner(ServiceProvider prov) => 
      new FunctionRunner(new SelfHostChangesNotifier(), prov.GetRequiredService<ICtlRepository>(), prov.GetRequiredService<CentazioSettings>());

  private Timer StartHost(List<IRunnableFunction> functions, IFunctionRunner runner) {
    return new Timer(RunFunctions, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    // ReSharper disable once AsyncVoidMethod
    async void RunFunctions(object? state) => await functions
        .Select(async f => await runner.RunFunction(f))
        .Synchronous();
  }

  private void DisplayInstructions() {
    Console.WriteLine("\nPress 'Enter' to exit\n\n");
    Console.ReadLine();
  }

}

public class SelfHostChangesNotifier : IChangesNotifier {

  // todo: implement
  public Task Notify(List<ObjectName> objs) {
    return Task.CompletedTask;
  }

}
