using Centazio.Providers.EF.Tests.E2E;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.Sqlite.Tests.E2E;

public class SqliteSimulationCoreStorageDbContext() : AbstractSimulationCoreStorageDbContext(DEFAULT_CORE_SCHEMA_NAME, nameof(Core.Ctl).ToLower()) {
  protected override void ConfigureDbSpecificOptions(DbContextOptionsBuilder options) => options.UseSqlite($"Data Source=core_storage.db");
}