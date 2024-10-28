using Centazio.Test.Lib;
using Centazio.Test.Lib.BaseProviderTests;

namespace Centazio.Providers.Sqlite.Tests.CoreRepo;

// todo: this test uses the old SQL Based TestingSqliteCoreStorageRepository which has been replaced by Ef-Core.
//   remove of replace with a proper EF-Core based solution
public class SqliteCoreRepositoryTests() : BaseCoreStorageRepositoryTests(false) {
  
  protected override async Task<ICoreStorageWithQuery> GetRepository() => await new TestingSqliteCoreStorageRepository().Initalise(new SqliteDbFieldsHelper());
}

