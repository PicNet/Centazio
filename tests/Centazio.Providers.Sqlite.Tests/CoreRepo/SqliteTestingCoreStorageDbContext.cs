using Centazio.Providers.EF.Tests.CoreRepo;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.Sqlite.Tests.CoreRepo;

public class SqliteTestingCoreStorageDbContext(string connstr) : AbstractTestingCoreStorageDbContext(DEFAULT_CORE_SCHEMA_NAME, nameof(Ctl).ToLower()) {

  protected override void ConfigureDbSpecificOptions(DbContextOptionsBuilder options) => options.UseSqlite(connstr);

}