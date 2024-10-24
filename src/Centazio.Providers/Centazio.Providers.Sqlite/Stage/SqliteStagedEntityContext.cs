using Centazio.Core.Ctl.Entities;
using Centazio.Providers.EF;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.Sqlite.Stage;

public class SqliteStagedEntityContext() : AbstractStagedEntityRepositoryDbContext(nameof(Core.Ctl).ToLower(), nameof(StagedEntity).ToLower()) {
  protected override void ConfigureDbSpecificOptions(DbContextOptionsBuilder options) => options.UseSqlite($"Data Source=staged_entity.db");
}