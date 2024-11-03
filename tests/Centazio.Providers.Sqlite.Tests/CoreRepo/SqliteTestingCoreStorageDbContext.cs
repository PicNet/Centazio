using Centazio.Providers.EF.Tests.CoreRepo;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.Sqlite.Tests.CoreRepo;

public class SqliteTestingCoreStorageDbContext() : AbstractTestingCoreStorageDbContext(DEFAULT_CORE_SCHEMA_NAME, nameof(Ctl).ToLower()) {

  protected override void ConfigureDbSpecificOptions(DbContextOptionsBuilder options) => options.UseSqlite($"Data Source=core_storage.db");

}