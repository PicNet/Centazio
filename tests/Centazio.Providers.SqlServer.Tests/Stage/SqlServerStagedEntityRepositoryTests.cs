using Centazio.Core.Checksum;
using Centazio.Core.Stage;
using Centazio.Providers.EF;
using Centazio.Providers.SqlServer.Stage;
using Centazio.Test.Lib.BaseProviderTests;

namespace Centazio.Providers.SqlServer.Tests.Stage;

public class SqlServerStagedEntityRepositoryTests : BaseStagedEntityRepositoryTests {
  protected override async Task<IStagedEntityRepository> GetRepository(int limit, Func<string, StagedEntityChecksum> checksum) {
    var connstr = await SqlConn.Instance.ConnStr();
    var opts = new EFCoreStagedEntityRepositoryOptions(limit, checksum, () => new SqlServerStagedEntityContext(connstr));
    return await new EFCoreStagedEntityRepository(opts).Initialise(new SqlServerDbFieldsHelper());
  }

}

