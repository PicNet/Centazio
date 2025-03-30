using Centazio.Providers.EF;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.PostgresSql.Ctl;

public class PostgresSqlCtlRepositoryDbContext(string connstr, string schemanm, string systemstatenm, string objectstatenm, string coretosysmapnm) : 
    AbstractCtlRepositoryDbContext(schemanm, systemstatenm, objectstatenm, coretosysmapnm) {
  
  protected override void ConfigureDbSpecificOptions(DbContextOptionsBuilder options) => 
      options.UseNpgsql(connstr);

}