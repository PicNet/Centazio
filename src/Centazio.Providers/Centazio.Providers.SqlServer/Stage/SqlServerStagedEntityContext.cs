using Centazio.Providers.EF;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.SqlServer.Stage;

public class SqlServerStagedEntityContext(string connstr, string schemanm, string tablenm) : AbstractStagedEntityRepositoryDbContext(schemanm, tablenm) {
  
  protected override void ConfigureDbSpecificOptions(DbContextOptionsBuilder options) => options.UseSqlServer(connstr);
}