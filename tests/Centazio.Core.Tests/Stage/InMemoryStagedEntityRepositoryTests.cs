using Centazio.Core.Checksum;
using Centazio.Core.Stage;
using Centazio.Test.Lib.BaseProviderTests;
using Centazio.Test.Lib.InMemRepos;

namespace Centazio.Core.Tests.Stage;

public class InMemoryStagedEntityRepositoryTests : BaseStagedEntityRepositoryTests {
  protected override Task<IStagedEntityRepository> GetRepository(int limit, Func<string, StagedEntityChecksum> checksum) => 
      Task.FromResult<IStagedEntityRepository>(new InMemoryStagedEntityRepository(limit, checksum));
}