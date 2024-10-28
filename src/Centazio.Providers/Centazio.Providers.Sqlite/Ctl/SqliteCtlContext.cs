using Centazio.Core.Ctl.Entities;
using Centazio.Providers.EF;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.Sqlite.Ctl;

public class SqliteCtlContext() : AbstractCtlRepositoryDbContext(nameof(Core.Ctl).ToLower(), nameof(SystemState).ToLower(), nameof(ObjectState).ToLower(), nameof(Map.CoreToSysMap).ToLower()) {
  protected override void ConfigureDbSpecificOptions(DbContextOptionsBuilder builder) {
    var fn = $"Data Source=centazio_ctl.db";
    builder.UseSqlite(fn);
  }

}