using Centazio.Providers.EF;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.PostgresSql;

public abstract class PostgresSqlDbContext(string connstr) : CentazioDbContext {
  protected override void ConfigureDbSpecificOptions(DbContextOptionsBuilder options) => 
      options.UseNpgsql(connstr);
}