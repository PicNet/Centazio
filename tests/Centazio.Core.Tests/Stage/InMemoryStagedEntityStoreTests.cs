using Centazio.Core.Checksum;
using Centazio.Core.Stage;
using Centazio.Test.Lib;
using Centazio.Test.Lib.BaseProviderTests;
using Centazio.Test.Lib.InMemRepos;

namespace Centazio.Core.Tests.Stage;

public class InMemoryStagedEntityRepositoryTests : StagedEntityRepositoryDefaultTests {
  protected override Task<IStagedEntityRepository> GetRepository(int limit = 0, Func<string, StagedEntityChecksum>? checksum = null) => 
      Task.FromResult<IStagedEntityRepository>(new InMemoryStagedEntityRepository(limit, checksum ?? Helpers.TestingStagedEntityChecksum));
}