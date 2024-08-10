using Centazio.Core.Stage;

namespace centazio.core.tests.Stage;

public class InMemoryStagedEntityStoreTests : StagedEntityStoreDefaultTests {
  protected override Task<IStagedEntityStore> GetStore() => Task.FromResult(new InMemoryStagedEntityStore() as IStagedEntityStore);
}