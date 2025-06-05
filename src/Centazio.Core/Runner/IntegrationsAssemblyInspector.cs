using U = Centazio.Core.Misc.ReflectionUtils;

namespace Centazio.Core.Runner;

public static class IntegrationsAssemblyInspector {

  public static IIntegrationBase GetCentazioIntegration(Assembly assembly, params List<string> environments) {
    return ValidateIntegrationFound(U.GetAllTypesThatImplement(typeof(IntegrationBase<,>), assembly));
    
    IIntegrationBase ValidateIntegrationFound(List<Type> integrations) {
      if (!integrations.Any()) throw new Exception($"Could not find the Centazio Integration in provided assembly[{assembly.GetName().FullName}]");
      if (integrations.Count > 1) throw new Exception($"Found {integrations.Count} Centazio Integrations in assembly[{assembly.GetName().FullName}].  There should only ever be one Integration per deployment unit");
      var integration = integrations.Single();
      var ctors = integration.GetConstructors(); 
      if (ctors.Length != 1) throw new Exception($"Integration[{integration.Name}] in assembly[{assembly.GetName().FullName}] must have a single constructor");
      return (IIntegrationBase) (ctors.Single().Invoke([environments]) ?? throw new Exception());
    }
  }

  public static Type GetCoreServiceFactoryType<F>(string provider) {
    var asses = U.GetProviderAssemblies();
    var potentials = asses.SelectMany(ass => U.GetAllTypesThatImplement(typeof(F), ass)).ToList();
    return ValidateCoreServiceFound<F>(provider, potentials);
  }

  private static Type ValidateCoreServiceFound<F>(string provider, List<Type> potentials) => 
      potentials.SingleOrDefault(type => type.Name.StartsWith(provider, StringComparison.OrdinalIgnoreCase)) 
          ?? throw new Exception($"Could not find {provider} provider for service {typeof(F).Name}.  Available provider types [{String.Join(",", potentials.Select(t => t.Name))}]");

  public static List<Type> GetRequiredCentazioFunctions(Assembly assembly, List<string> filters) {
    var functions = GetCentazioFunctions(assembly, filters);
    if (!functions.Any()) throw new Exception($"Could not find any Centazio Functions in assembly[{assembly.GetName().Name}] matching filters[{String.Join(',', filters)}]");
    return functions;
  }
  
  public static List<Type> GetCentazioFunctions(Assembly assembly, List<string> filters) {
    return U.GetAllTypesThatImplement(typeof(AbstractFunction<>), assembly).Where(DoesTypeMatchFilter).ToList();
    
    bool DoesTypeMatchFilter(Type type) => !filters.Any() || filters.Contains("All", StringComparer.OrdinalIgnoreCase) 
        || filters.Any(filter => (type.FullName ?? String.Empty).Contains(filter, StringComparison.OrdinalIgnoreCase));
  }
  
  public static IRunnableFunction CreateFuncWithNullCtorArgs(Type functype) {
    return functype.GetConstructors().Select(ctor => {
      var args = ctor.GetParameters().Select(_ => null as object).ToArray();
      try { return (IRunnableFunction) ctor.Invoke(args); }
      catch { return null; }
    }).First(func => func is not null) ?? throw new Exception($"Could not create a dummy instance of function [{functype.FullName}]");
  }
}