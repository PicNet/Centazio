using Centazio.Core.Stage;
using Centazio.Providers.EF;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.Sqlite.Stage;

public class SqliteStagedEntityContext(string connstr) : AbstractStagedEntityRepositoryDbContext(nameof(Core.Ctl).ToLower(), nameof(StagedEntity).ToLower()) {
  protected override void ConfigureDbSpecificOptions(DbContextOptionsBuilder options) => SqliteEfContextInitialiser.SetSqliteOnDbContextOpts(options, connstr);
}