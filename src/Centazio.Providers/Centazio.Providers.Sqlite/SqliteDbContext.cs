using Centazio.Providers.EF;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.Sqlite;

public class SqliteDbContext(string connstr, Action<ModelBuilder> buildmodel) : CentazioDbContext {

  // todo: if this approach is good, then move CreateCentazioModel to CentazioDbContext
  protected override void CreateCentazioModel(ModelBuilder builder) => buildmodel(builder);
  protected override void ConfigureDbSpecificOptions(DbContextOptionsBuilder options) => options.UseSqlite(connstr);
}