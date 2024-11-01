using Centazio.Core.Ctl.Entities;
using Centazio.Providers.EF;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.Sqlite.Ctl;

public class SqliteCtlRepositoryDbContext() : AbstractCtlRepositoryDbContext(nameof(Core.Ctl).ToLower(), nameof(SystemState).ToLower(), nameof(ObjectState).ToLower(), nameof(Map.CoreToSysMap).ToLower()) {
  protected override void ConfigureDbSpecificOptions(DbContextOptionsBuilder options) {
    var fn = $"Data Source=centazio_ctl.db";
    options.UseSqlite(fn);
  }

}