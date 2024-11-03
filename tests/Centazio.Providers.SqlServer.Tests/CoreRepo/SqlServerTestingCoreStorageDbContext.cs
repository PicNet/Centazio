using Centazio.Providers.EF.Tests.CoreRepo;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.SqlServer.Tests.CoreRepo;

public class SqlServerTestingCoreStorageDbContext(string connstr) : AbstractTestingCoreStorageDbContext(DEFAULT_CORE_SCHEMA_NAME, nameof(Ctl).ToLower()) {

  protected override void ConfigureDbSpecificOptions(DbContextOptionsBuilder options) => options.UseSqlServer(connstr);

}