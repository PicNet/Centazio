using System.Reflection;
using U = Centazio.Core.Misc.ReflectionUtils;

namespace Centazio.Core.Runner;

public static class IntegrationsAssemblyInspector {

  public static IIntegrationBase GetCentazioIntegration(Assembly assembly) => 
      ValidateIntegrationFound(U.GetAllTypesThatImplement(typeof(IntegrationBase<,>), assembly));

  private static IIntegrationBase ValidateIntegrationFound(List<Type> integrations) {
    if (!integrations.Any()) throw new Exception("Could not find the Centazio Integration in provided assemblies");
    if (integrations.Count > 1) throw new Exception($"Found {integrations.Count} Centazio Integrations in the provided assemblies.  There should only ever be one Integration per deployment unit");
    return (IIntegrationBase) (Activator.CreateInstance(integrations.Single()) ?? throw new Exception());
  }

  public static Type GetCoreServiceFactoryType<F>(string provider) {
    var asses = U.GetProviderAssemblies();
    var potentials = asses.SelectMany(ass => U.GetAllTypesThatImplement(typeof(F), ass)).ToList();
    return ValidateCoreServiceFound<F>(provider, potentials);
  }

  private static Type ValidateCoreServiceFound<F>(string provider, List<Type> potentials) => 
      potentials.SingleOrDefault(type => type.Name.StartsWith(provider, StringComparison.OrdinalIgnoreCase)) 
          ?? throw new Exception($"Could not find {typeof(F).Name} of provider type [{provider}].  Found provider types [{String.Join(",", potentials.Select(t => t.Name))}]");

  public static List<Type> GetCentazioFunctions(Assembly assembly, List<string> filters) {
    var functions = U.GetAllTypesThatImplement(typeof(AbstractFunction<>), assembly).Where(DoesTypeMatchFilter).ToList();
    if (!functions.Any()) throw new Exception($"Could not find any Centazio Functions in assembly[{assembly.GetName().Name}] matching filters[{String.Join(',', filters)}]");
    return functions;
    
    bool DoesTypeMatchFilter(Type type) => !filters.Any() || filters.Contains("All", StringComparer.OrdinalIgnoreCase) 
        || filters.Any(filter => (type.FullName ?? String.Empty).Contains(filter, StringComparison.OrdinalIgnoreCase));
  }

}