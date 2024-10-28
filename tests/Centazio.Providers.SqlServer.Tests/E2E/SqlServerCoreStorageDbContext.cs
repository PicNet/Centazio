using Centazio.Providers.EF.Tests.E2E;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.SqlServer.Tests.E2E;

public class SqlServerCoreStorageDbContext(string connstr) : AbstractCoreStorageDbContext("dbo") {
  protected override void ConfigureDbSpecificOptions(DbContextOptionsBuilder options) => options.UseSqlServer(connstr);
}