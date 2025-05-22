using Centazio.Providers.EF;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.PostgresSql.Ctl;

public class PostgresSqlCtlRepositoryDbContext(string connstr, string schemanm, string systemstatenm, string objectstatenm, string coretosysmapnm, string entitychangenm) : 
    AbstractCtlRepositoryDbContext(schemanm, systemstatenm, objectstatenm, coretosysmapnm, entitychangenm) {
  
  protected override void ConfigureDbSpecificOptions(DbContextOptionsBuilder options) => 
      options.UseNpgsql(connstr);

}