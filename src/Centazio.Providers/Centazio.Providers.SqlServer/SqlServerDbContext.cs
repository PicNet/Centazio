using Centazio.Providers.EF;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.SqlServer;

public class SqlServerDbContext(string connstr, Action<ModelBuilder> buildmodel) : CentazioDbContext {

  // todo: if this approach is good, then move CreateCentazioModel to CentazioDbContext and use for all repositories (staging, ctl, etc)
  protected override void CreateCentazioModel(ModelBuilder builder) => buildmodel(builder);
  protected override void ConfigureDbSpecificOptions(DbContextOptionsBuilder options) => options.UseSqlServer(connstr);
}