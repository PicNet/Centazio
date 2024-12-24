using Centazio.Providers.EF;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.Sqlite.Ctl;

public class SqliteCtlRepositoryDbContext(string connstr, string schemanm, string systemstatenm, string objectstatenm, string coretosysmapnm) : 
    AbstractCtlRepositoryDbContext(schemanm, systemstatenm, objectstatenm, coretosysmapnm) {
  
  protected override void ConfigureDbSpecificOptions(DbContextOptionsBuilder options) => options.UseSqlite(connstr);

}