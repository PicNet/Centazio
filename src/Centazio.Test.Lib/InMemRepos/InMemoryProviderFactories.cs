using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Core.Stage;

namespace Centazio.Test.Lib.InMemRepos;

// selected with `Provider: "InMemory"` (see `settings.sim.json`) when the Test.Lib assembly is one of the engine's
//    provider assemblies, i.e. when using the `InMemoryCentazioEngine`

public class InMemoryStagedEntityRepositoryFactory(StagedEntityRepositorySettings settings, TimeProvider clock, IGuidGenerator ids) : IServiceFactory<IStagedEntityRepository> {
  public IStagedEntityRepository GetService() => new InMemoryStagedEntityRepository(settings.Limit, new Sha256ChecksumAlgorithm().Checksum, clock, ids);
}

public class InMemoryCtlRepositoryFactory(TimeProvider clock) : IServiceFactory<ICtlRepository> {
  public ICtlRepository GetService() => new InMemoryBaseCtlRepository(clock);
}

public record InMemorySecrets(Dictionary<string, string> Values);

public class InMemorySecretsLoaderFactory(InMemorySecrets secrets) : IServiceFactory<ISecretsLoader> {
  public ISecretsLoader GetService() => new InMemorySecretsLoader(secrets);
}

public class InMemorySecretsLoader(InMemorySecrets secrets) : AbstractSecretsLoader {
  protected override Task<Dictionary<string, string>> LoadSecretsAsDictionaryForEnvironment(string environment, bool required) => Task.FromResult(secrets.Values);
}
