using Centazio.Providers.EF.Tests.CoreRepo;
using Centazio.Providers.EF.Tests.E2E;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.Sqlite.Tests.CoreRepo;

public class SqliteTestingCoreStorageDbContext() : AbstractTestingCoreStorageDbContext("core") {

  protected override void ConfigureDbSpecificOptions(DbContextOptionsBuilder options) => options.UseSqlite($"Data Source=core_storage.db");

}