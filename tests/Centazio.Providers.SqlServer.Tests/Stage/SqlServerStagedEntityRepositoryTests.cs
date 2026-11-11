using Centazio.Core.Checksum;
using Centazio.Core.Misc;
using Centazio.Core.Stage;
using Centazio.Providers.EF;
using Centazio.Providers.SqlServer.Stage;
using Centazio.Test.Lib.BaseProviderTests;

namespace Centazio.Providers.SqlServer.Tests.Stage;

public class SqlServerStagedEntityRepositoryTests : BaseStagedEntityRepositoryTests {
  protected override async Task<IStagedEntityRepository> GetRepository(int limit, Func<string, StagedEntityChecksum> checksum, TimeProvider clock, IGuidGenerator ids) {
    var connstr = (await SqlConn.GetInstance(false, await F.Secrets())).ConnStr;
    var settings = (await F.Settings()).StagedEntityRepository with { ConnectionString = connstr };
    var opts = new EfStagedEntityRepositoryOptions(limit, checksum, () => new SqlServerStagedEntityContext(settings), clock, ids);
    return await new EfStagedEntityRepository(opts, true).Initialise();
  }
}

