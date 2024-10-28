using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Providers.EF;
using Centazio.Providers.Sqlite.Ctl;
using Centazio.Test.Lib;
using Centazio.Test.Lib.BaseProviderTests;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.Sqlite.Tests.Ctl;

public class SqliteCtlRepositoryTests : CtlRepositoryDefaultTests {
  protected override async Task<ICtlRepository> GetRepository() => await new TestingSqliteCtlRepository().Initalise(new SqliteDbFieldsHelper());
}

public class SqliteCtlRepoMappingsTests : BaseCtlRepoMappingsTests {
  protected override async Task<ITestingCtlRepository> GetRepository() => (ITestingCtlRepository) await new TestingSqliteCtlRepository().Initalise(new SqliteDbFieldsHelper());
}

internal class TestingSqliteCtlRepository() : EFCoreCtlRepository(() => new SqliteCtlContext()), ITestingCtlRepository {

  public async Task<List<Map.CoreToSysMap>> GetAllMaps() {
    await using var conn = new SqliteCtlContext();
    return (await conn.CoreToSystemMaps.ToListAsync()).Select(dto => dto.ToBase()).ToList();
  }
}