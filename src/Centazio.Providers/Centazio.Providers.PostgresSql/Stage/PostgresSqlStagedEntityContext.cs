using Centazio.Core.Settings;
using Centazio.Providers.EF;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.PostgresSql.Stage;

public class PostgresSqlStagedEntityContext(StagedEntityRepositorySettings settings) : AbstractStagedEntityRepositoryDbContext(settings) {
  protected override void ConfigureDbSpecificOptions(DbContextOptionsBuilder options) => 
      options.UseNpgsql(Settings.ConnectionString);

  protected override string ToTableName(TableName table) => $"{table.Schema}.{table.Table}";

}