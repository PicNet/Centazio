using Centazio.Core.Checksum;
using Centazio.Core.Stage;
using Centazio.Providers.EF;
using Centazio.Providers.EF.Tests;
using Centazio.Providers.SqlServer.Stage;
using Centazio.Test.Lib.BaseProviderTests;

namespace Centazio.Providers.SqlServer.Tests.Stage;

public class SqlServerStagedEntityRepositoryTests : BaseStagedEntityRepositoryTests {
  protected override async Task<IStagedEntityRepository> GetRepository(int limit, Func<string, StagedEntityChecksum> checksum) {
    var connstr = (await SqlConn.GetInstance(false, await F.Secrets())).ConnStr;
    var settings = (await F.Settings()).StagedEntityRepository with { ConnectionString = connstr };
    var opts = new EFStagedEntityRepositoryOptions(limit, checksum, () => new SqlServerStagedEntityContext(settings));
    return await new TestingEfStagedEntityRepository(opts).Initialise();
  }

}

