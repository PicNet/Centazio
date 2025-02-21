using Centazio.Providers.EF;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.Sqlite;

public abstract class SqliteDbContext(string connstr) : CentazioDbContext {
  protected override void ConfigureDbSpecificOptions(DbContextOptionsBuilder options) => SqliteEfContextInitialiser.SetSqliteOnDbContextOpts(options, connstr);
}