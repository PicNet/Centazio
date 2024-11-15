﻿using Centazio.Core.Ctl.Entities;
using Centazio.Providers.EF;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.Sqlite.Stage;

public class SqliteStagedEntityContext(string dbfile) : AbstractStagedEntityRepositoryDbContext(nameof(Core.Ctl).ToLower(), nameof(StagedEntity).ToLower()) {
  protected override void ConfigureDbSpecificOptions(DbContextOptionsBuilder options) => options.UseSqlite($"Data Source={dbfile}");
}