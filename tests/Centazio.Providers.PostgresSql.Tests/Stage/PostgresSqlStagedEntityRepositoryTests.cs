using Centazio.Core.Checksum;
using Centazio.Core.Stage;
using Centazio.Providers.EF.Tests;
using Centazio.Providers.PostgresSql.Stage;
using Centazio.Test.Lib.BaseProviderTests;

namespace Centazio.Providers.PostgresSql.Tests.Stage;

public class PostgresSqlStagedEntityRepositoryTests : BaseStagedEntityRepositoryTests {
  protected override async Task<IStagedEntityRepository> GetRepository(int limit, Func<string, StagedEntityChecksum> checksum) {
    var connstr = await new PostgresSqlConnection().Init();
    var settings = (await F.Settings()).StagedEntityRepository with { ConnectionString = connstr };
    return await new TestingEfStagedEntityRepository(new(limit, checksum, () => new PostgresSqlStagedEntityContext(settings)), new PostgresSqlDbFieldsHelper()).Initialise();
  }

}

