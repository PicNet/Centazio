using Centazio.Core;
using Centazio.Core.Ctl;
using Centazio.Core.Settings;
using Centazio.Providers.EF;

namespace Centazio.Providers.SqlServer.Ctl;

public class SqlServerCtlRepositoryFactory(CtlRepositorySettings settings) : IServiceFactory<ICtlRepository> {
  public ICtlRepository GetService() => 
      new SqlServerCtlRepository(Getdb, settings.CreateSchema);
  
  private AbstractCtlRepositoryDbContext Getdb() => new SqlServerCtlRepositoryDbContext(settings);
}

public class SqlServerCtlRepository(Func<AbstractCtlRepositoryDbContext> getdb, bool createschema) : AbstractEFCtlRepository(getdb) {
  
  public override async Task<ICtlRepository> Initialise() {
    if (!createschema) return this;
    
    return await UseDb(async db => {
      await CreateSchema(db);
      return this;
    });
  }

}