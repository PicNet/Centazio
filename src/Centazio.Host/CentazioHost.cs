using Centazio.Core.Misc;
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
