using Centazio.Core.Ctl.Entities;
using Centazio.Providers.EF;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.SqlServer.Stage;

public class SqlServerStagedEntityContext(string connstr) : AbstractStagedEntityRepositoryDbContext(nameof(Core.Ctl).ToLower(), nameof(StagedEntity).ToLower()) {
  
  protected override void ConfigureDbSpecificOptions(DbContextOptionsBuilder options) => options.UseSqlServer(connstr);
}