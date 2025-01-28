using Centazio.Core.Runner;
using Centazio.Core.Settings;
using Timer = System.Threading.Timer;

namespace Centazio.Host;

public record HostSettings(string FunctionFilter, CentazioSettings CoreSettings) {
  public List<string> ParseFunctionFilters() => FunctionFilter.Split([',', ';', '|', ' ']).Select(f => f.Trim()).Where(f => !String.IsNullOrEmpty(f)).ToList();
}

public class CentazioHost(HostSettings settings, bool quiet) {
  
  public async Task Run() {
    var functions = await new HostBootstrapper(settings.CoreSettings, quiet)
        .InitHost(settings.ParseFunctionFilters());
    
    await using var timer = StartHost(functions);
    DisplatInstructions();
  }

  private Timer StartHost(List<IRunnableFunction> functions) {
    return new Timer(RunFunctions, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    async void RunFunctions(object? state) => await Task.WhenAll(functions.Select(async f => await f.RunFunction()));
  }

  private void DisplatInstructions() {
    Console.WriteLine("\nPress 'Enter' to exit\n\n");
    Console.ReadLine();
  }

}
