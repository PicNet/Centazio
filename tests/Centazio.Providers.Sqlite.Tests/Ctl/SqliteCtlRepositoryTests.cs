using Centazio.Core.Ctl;
using Centazio.Providers.Sqlite.Ctl;
using Centazio.Test.Lib.AbstractProviderTests;

namespace Centazio.Providers.Sqlite.Tests.Ctl;

public class SqliteCtlRepositoryTests : CtlRepositoryDefaultTests {

  protected override async Task<ICtlRepository> GetRepository() 
      => await new TestingSqliteCtlRepository().Initalise();

  class TestingSqliteCtlRepository() : SqliteCtlRepository(SqliteConn.Instance.Conn);
  
  
}