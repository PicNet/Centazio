using Centazio.Core.Stage;
using Centazio.Test.Lib;
using Centazio.Test.Lib.AbstractProviderTests;

namespace Centazio.Core.Tests.Stage;

public class InMemoryStagedEntityStoreTests : StagedEntityStoreDefaultTests {
  protected override Task<IStagedEntityStore> GetStore(int limit = 0, Func<string, string>? checksum = null) => 
      Task.FromResult<IStagedEntityStore>(new InMemoryStagedEntityStore(limit, checksum ?? Helpers.TestingChecksum));
}