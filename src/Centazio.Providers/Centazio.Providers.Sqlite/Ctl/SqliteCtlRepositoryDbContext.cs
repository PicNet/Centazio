using Centazio.Core.Settings;
using Centazio.Providers.EF;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.Sqlite.Ctl;

public class SqliteCtlRepositoryDbContext(CtlRepositorySettings settings) : AbstractCtlRepositoryDbContext(settings) {
  protected override void ConfigureDbSpecificOptions(DbContextOptionsBuilder options) => SqliteDbContext.SetSqliteOnDbContextOpts(options, Settings.ConnectionString);
}