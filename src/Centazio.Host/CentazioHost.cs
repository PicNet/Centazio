using System.Reflection;
using Centazio.Core.Misc;
using Centazio.Core.Runner;

namespace Centazio.Host;

public record HostSettings(string FunctionFilter);

public class CentazioHost(HostSettings settings) {
  
  public Task Run() {
    var filters = settings.FunctionFilter.Split([',', ';', '|', ' ']).Select(f => f.Trim()).Where(f => !String.IsNullOrEmpty(f)).ToList();
    var functions = GetFunctionsToRun(filters);
    Console.WriteLine($"Found Functions [{settings.FunctionFilter}]({functions.Count}):\n\t" + String.Join("\n\t", functions.Select(f => f.FullName)));
    return Task.CompletedTask;
  }

  private List<Type> GetFunctionsToRun(List<string> filters) {
    var ignore = new [] {"AWSSDK", "Microsoft", "Azure", "nunit", "Serilog", "System", "Testcontainers", "Cronos", "Docker", "Centazio.Providers", "Centazio.Core", "Centazio.Test", "Centazio.Cli", "e_sqlite3"};
    var root = ReflectionUtils.GetSolutionRootDirectory();
    var done = new Dictionary<string, bool>();
    return Directory.GetFiles(root, "*.dll", SearchOption.AllDirectories).SelectMany(dll => {
      var assname = dll.Split('\\').Last();
      if (ignore.Any(i => assname.StartsWith(i, StringComparison.OrdinalIgnoreCase))) return [];
      if (!done.TryAdd(assname, true)) return [];
      return Assembly.LoadFrom(dll).GetTypes()
          .Where(type => 
              type.FullName is not null && 
              !type.IsAbstract && 
              MatchesFilter(type.FullName) && 
              IsCentazioFunction(type));
    }).ToList();
    
    bool MatchesFilter(string name) => filters.Contains("all", StringComparer.OrdinalIgnoreCase) 
        || filters.Any(filter => name.Contains(filter, StringComparison.OrdinalIgnoreCase));
    
    bool IsCentazioFunction(Type typ) => 
      (typ.IsGenericType && typ.GetGenericTypeDefinition() == typeof(AbstractFunction<,>))
      || (typ.BaseType is not null && IsCentazioFunction(typ.BaseType));
  }
}
