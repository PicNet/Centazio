using Centazio.Core.Stage;

namespace Centazio.Core.Tests.Stage;

public class InMemoryStagedEntityStoreTests : StagedEntityStoreDefaultTests {
  protected override Task<IStagedEntityStore> GetStore(int limit = 0, Func<string, string>? checksum = null) => 
      Task.FromResult(new InMemoryStagedEntityStore(limit, checksum ?? TestingFactories.TestingChecksum) as IStagedEntityStore);
}