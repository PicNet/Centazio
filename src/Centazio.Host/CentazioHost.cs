using System.Timers;
using Centazio.Core;
using Centazio.Core.Ctl;
using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Core.Stage;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Timer = System.Threading.Timer;

namespace Centazio.Host;

public record HostSettings(string FunctionFilter, CentazioSettings CoreSettings, CentazioSecrets Secrets) {
  public List<string> ParseFunctionFilters() => FunctionFilter.Split([',', ';', '|', ' ']).Select(f => f.Trim()).Where(f => !String.IsNullOrEmpty(f)).ToList();
}

public class CentazioHost(HostSettings settings) {
  
  public async Task Run() {
    Log.Logger = LogInitialiser
        .GetFileAndConsoleConfig()
        .CreateLogger();
    var types = GetCentazioFunctionTypess(settings.ParseFunctionFilters());
    Log.Information($"CentazioHost found functions:\n\t" + String.Join("\n\t", types.Select(type => type.Name)));
    var prov = InitialiseDi(types);
    await InitialiseCoreServices(prov);
    var functions = InitialiseFunctions(prov, types);

    await using var timer = new Timer(_ => functions.ForEach(RunFunction), null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
    Console.WriteLine("press 'Enter' to exit");
    Console.ReadLine();
  }
  
  private void RunFunction(object function) {
    Log.Information($"running function[{function.GetType().Name}]");
  }

  private List<Type> GetCentazioFunctionTypess(List<string> filters) {
    return ReflectionUtils.GetAllTypesThatImplement(typeof(AbstractFunction<,>), settings.CoreSettings.AllowedFunctionAssemblies).Where(type => MatchesFilter(type.FullName ?? String.Empty)).ToList();
    bool MatchesFilter(string name) => filters.Contains("all", StringComparer.OrdinalIgnoreCase) 
        || filters.Any(filter => name.Contains(filter, StringComparison.OrdinalIgnoreCase));
  }

  private ServiceProvider InitialiseDi(List<Type> functypes) {
    var svcs = new ServiceCollection();
    svcs.AddSingleton(settings.CoreSettings);
    
    AddService<IServiceFactory<IStagedEntityRepository>, IStagedEntityRepository>(settings.CoreSettings.StagedEntityRepository.Provider);
    AddService<IServiceFactory<ICtlRepository>, ICtlRepository>(settings.CoreSettings.CtlRepository.Provider);
    
    functypes.ForEach(type => svcs.AddSingleton(type));
    ReflectionUtils.GetAllTypesThatImplement(typeof(IFunctionInitialiser), settings.CoreSettings.AllowedFunctionAssemblies).ForEach(type => {
      var initialiser = Activator.CreateInstance(type) as IFunctionInitialiser ?? throw new Exception();
      initialiser.RegisterServices(svcs);
    });
    return svcs.BuildServiceProvider();
    
    void AddService<F, I>(string provider) where F : IServiceFactory<I> where I : class {
      svcs.AddSingleton(typeof(F), GetFactoryType<F>(provider));
      svcs.AddSingleton<I>(prov => prov.GetRequiredService<F>().GetService());
    }
    
    Type GetFactoryType<F>(string provider) {
      var potentials = ReflectionUtils.GetAllTypesThatImplement(typeof(F), settings.CoreSettings.AllowedFunctionAssemblies); 
      return potentials.SingleOrDefault(type => type.Name.StartsWith(provider, StringComparison.OrdinalIgnoreCase)) 
          ?? throw new Exception($"Could not find {typeof(F).Name} of provider type [{provider}].  Found provider types [{String.Join(",", potentials.Select(t => t.Name))}]");
    }
    
  }

  private async Task InitialiseCoreServices(ServiceProvider prov) {
    await prov.GetRequiredService<IStagedEntityRepository>().Initialise();
    await prov.GetRequiredService<ICtlRepository>().Initialise();
  }
  
  // todo: add Runnable interface
  private List<object> InitialiseFunctions(ServiceProvider svcs, List<Type> types) {
    return types.Select(svcs.GetRequiredService).ToList();
  }

}
