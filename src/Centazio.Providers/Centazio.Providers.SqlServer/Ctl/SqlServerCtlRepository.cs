using Centazio.Core;
using Centazio.Core.Ctl;
using Centazio.Core.Misc;
using Centazio.Core.Settings;
using Centazio.Providers.EF;

namespace Centazio.Providers.SqlServer.Ctl;

public class SqlServerCtlRepositoryFactory(CtlRepositorySettings settings) : IServiceFactory<ICtlRepository> {
  public ICtlRepository GetService() => 
      new SqlServerCtlRepository(Getdb, new SqlServerDbFieldsHelper(), settings.CreateSchema);
  
  private AbstractCtlRepositoryDbContext Getdb() 
    => new SqlServerCtlRepositoryDbContext(settings.ConnectionString, settings.SchemaName, settings.SystemStateTableName, settings.ObjectStateTableName, settings.CoreToSysMapTableName, settings.EntityChangeTableName);
}

public class SqlServerCtlRepository(Func<AbstractCtlRepositoryDbContext> getdb, IDbFieldsHelper dbf, bool createschema) : EFCtlRepository(getdb) {
  
  public override async Task<ICtlRepository> Initialise() {
    if (!createschema) return this;
    
    await using var db = getdb();
    await CreateSchema(dbf, db);
    return this;
  }

}