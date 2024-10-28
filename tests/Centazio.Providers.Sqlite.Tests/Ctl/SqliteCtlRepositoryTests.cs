using Centazio.Core.Ctl;
using Centazio.Providers.EF.Tests;
using Centazio.Providers.Sqlite.Ctl;
using Centazio.Test.Lib;
using Centazio.Test.Lib.BaseProviderTests;

namespace Centazio.Providers.Sqlite.Tests.Ctl;

public class SqliteCtlRepositoryTests : CtlRepositoryDefaultTests {
  protected override async Task<ICtlRepository> GetRepository() => await new TestingEFCoreCtlRepository(() => new SqliteCtlContext(), new SqliteDbFieldsHelper()).Initalise();
}

public class SqliteCtlRepoMappingsTests : BaseCtlRepoMappingsTests {
  protected override async Task<ITestingCtlRepository> GetRepository() => (ITestingCtlRepository) await new TestingEFCoreCtlRepository(() => new SqliteCtlContext(), new SqliteDbFieldsHelper()).Initalise();
}
