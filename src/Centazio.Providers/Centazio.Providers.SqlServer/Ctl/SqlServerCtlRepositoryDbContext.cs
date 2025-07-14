using Centazio.Core.Settings;
using Centazio.Providers.EF;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.SqlServer.Ctl;

public class SqlServerCtlRepositoryDbContext(CtlRepositorySettings settings) : AbstractCtlRepositoryDbContext(settings) {
  protected override void ConfigureDbSpecificOptions(DbContextOptionsBuilder options) => options.UseSqlServer(Settings.ConnectionString);
  
  protected override string ToTableName(TableName table) => $"[{table.Schema}].[{table.Table}]";

}