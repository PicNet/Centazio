using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.PostgresSql;

public class PostgresSqlEfContextInitialiser {
  public static void SetPostgresSqlOnDbContextOpts(DbContextOptionsBuilder options, string connstr) {
    options.UseNpgsql(connstr);
  }
}