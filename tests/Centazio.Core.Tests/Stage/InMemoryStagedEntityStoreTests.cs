using Centazio.Core.Stage;

namespace Centazio.Core.Tests.Stage;

public class InMemoryStagedEntityStoreTests : StagedEntityStoreDefaultTests {
  protected override Task<IStagedEntityStore> GetStore(int limit = 0) => Task.FromResult(new InMemoryStagedEntityStore(limit) as IStagedEntityStore);
}