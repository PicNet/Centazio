using Centazio.Core.Settings;
using Centazio.Providers.EF;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.Sqlite.Stage;

public class SqliteStagedEntityContext(StagedEntityRepositorySettings settings) : AbstractStagedEntityRepositoryDbContext(settings) {
  protected override void ConfigureDbSpecificOptions(DbContextOptionsBuilder options) => SqliteDbContext.SetSqliteOnDbContextOpts(options, Settings.ConnectionString);
  protected override string ToTableName(TableName table) => $"[{table.Table}]";

}