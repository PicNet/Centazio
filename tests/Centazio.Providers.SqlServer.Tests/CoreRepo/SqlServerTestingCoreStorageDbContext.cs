using Centazio.Providers.EF.Tests.CoreRepo;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.SqlServer.Tests.CoreRepo;

public class SqlServerTestingCoreStorageDbContext(string connstr) : AbstractTestingCoreStorageDbContext("core") {

  protected override void ConfigureDbSpecificOptions(DbContextOptionsBuilder options) => options.UseSqlServer(connstr);

}