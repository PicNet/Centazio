using Centazio.Providers.EF;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.PostgresSql;

// todo: why do we need a separate DbContext and ContextInitialiser for all these providers
public abstract class PostgresSqlDbContext(string connstr) : CentazioDbContext {
  protected override void ConfigureDbSpecificOptions(DbContextOptionsBuilder options) => PostgresSqlEfContextInitialiser.SetPostgresSqlOnDbContextOpts(options, connstr);
}