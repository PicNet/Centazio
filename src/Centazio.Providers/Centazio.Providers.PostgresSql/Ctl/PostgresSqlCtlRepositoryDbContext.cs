using Centazio.Core.Settings;
using Centazio.Providers.EF;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.PostgresSql.Ctl;

public class PostgresSqlCtlRepositoryDbContext(CtlRepositorySettings settings) : AbstractCtlRepositoryDbContext(settings) {
  protected override void ConfigureDbSpecificOptions(DbContextOptionsBuilder options) => options.UseNpgsql(Settings.ConnectionString);
}