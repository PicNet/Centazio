using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Centazio.Core.Settings;
using Serilog.Events;
using Timer = System.Threading.Timer;

namespace Centazio.Host;

public interface IHostConfiguration {
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

// todo: clean up the handling of CentazioSettings, being registered as singleton, passed in methods, etc.
public record HostSettings(IHostConfiguration HostConfig, CentazioSettings CoreSettings);

public class CentazioHost(HostSettings settings) {
  
  public async Task Run() {
    var functions = await new HostBootstrapper(settings)
        .InitHost(settings.HostConfig.ParseFunctionFilters());
    
    await using var timer = StartHost(functions);
    DisplayInstructions();
  }

  private Timer StartHost(List<IRunnableFunction> functions) {
    return new Timer(RunFunctions, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    // ReSharper disable once AsyncVoidMethod
    async void RunFunctions(object? state) => await functions
        .Select(async f => await f.RunFunction())
        .Synchronous();
  }

  private void DisplayInstructions() {
    Console.WriteLine("\nPress 'Enter' to exit\n\n");
    Console.ReadLine();
  }

}
