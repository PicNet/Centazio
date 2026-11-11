using System.Reflection;
using Centazio.Core.Engine;
using Centazio.Core.Runner;
using Centazio.Core.Settings;
using Centazio.Test.Lib.InMemRepos;
using Microsoft.Extensions.DependencyInjection;

namespace Centazio.Test.Lib;

/// <summary>
/// A real `CentazioEngine` (settings, provider lookup, DI, integrations) whose core services come from the Test.Lib
/// in-memory repositories.  Use with the `sim` environment, whose settings select the `InMemory` providers.  Time and
/// ids come only from the given `clock` and `ids`, and function-to-function notifications are queued until
/// `ProcessPending` is called on the `InProcessChangesNotifier`.
/// </summary>
public class InMemoryCentazioEngine(List<string> environments, TimeProvider clock, IGuidGenerator ids, InMemorySecrets? secrets = null) : CentazioEngine(environments, clock) {

  protected override List<Assembly> ProviderAssemblies => base.ProviderAssemblies.Concat([typeof(InMemoryCentazioEngine).Assembly]).ToList();

  protected override void RegisterHostSpecificServices(CentazioServicesRegistrar registrar) {
    registrar.Register(ids);
    registrar.Register(secrets ?? new InMemorySecrets([]));
    registrar.Register<InProcessChangesNotifier>(_ => new InProcessChangesNotifier());
    registrar.Register<IChangesNotifier>(prov => prov.GetRequiredService<InProcessChangesNotifier>());
    registrar.Register<IFunctionRunner>(prov => {
      var inner = new FunctionRunner(prov.GetRequiredService<ICtlRepository>(), prov.GetRequiredService<CentazioSettings>(), prov.GetRequiredService<TimeProvider>(), true);
      return new FunctionRunnerWithNotificationAdapter(inner, prov.GetRequiredService<IChangesNotifier>());
    });
  }
}
