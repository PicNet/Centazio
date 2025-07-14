using Centazio.Core.Settings;
using Centazio.Providers.EF;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.SqlServer.Stage;

public class SqlServerStagedEntityContext(StagedEntityRepositorySettings settings) : AbstractStagedEntityRepositoryDbContext(settings) {
  
  protected override void ConfigureDbSpecificOptions(DbContextOptionsBuilder options) => options.UseSqlServer(Settings.ConnectionString);
  
  protected override string ToTableName(TableName table) => $"[{table.Schema}].[{table.Table}]";

}