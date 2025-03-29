using Centazio.Core.Stage;
using Centazio.Providers.EF;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.PostgresSql.Stage;

public class PostgresSqlStagedEntityContext(string connstr) : AbstractStagedEntityRepositoryDbContext(nameof(Core.Ctl).ToLower(), nameof(StagedEntity).ToLower()) {
  protected override void ConfigureDbSpecificOptions(DbContextOptionsBuilder options) => PostgresSqlEfContextInitialiser.SetPostgresSqlOnDbContextOpts(options, connstr);
}