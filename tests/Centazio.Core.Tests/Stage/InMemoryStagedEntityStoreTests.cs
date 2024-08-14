using Centazio.Core.Stage;

namespace centazio.core.tests.Stage;

public class InMemoryStagedEntityStoreTests : StagedEntityStoreDefaultTests {
  protected override Task<IStagedEntityStore> GetStore(int limit = 0) => Task.FromResult(new InMemoryStagedEntityStore(limit) as IStagedEntityStore);
}