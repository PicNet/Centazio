using Centazio.Providers.EF.Tests.E2E;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.SqlServer.Tests.E2E;

public class SqlServerSimulationCoreStorageDbContext(string connstr) : AbstractSimulationCoreStorageDbContext(DEFAULT_CORE_SCHEMA_NAME, nameof(Core.Ctl).ToLower()) {
  protected override void ConfigureDbSpecificOptions(DbContextOptionsBuilder options) => options.UseSqlServer(connstr);
}