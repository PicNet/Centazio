using Centazio.Core;
using Centazio.Core.Ctl;
using Centazio.Core.Settings;
using Centazio.Providers.EF;

namespace Centazio.Providers.PostgresSql.Ctl;

public class PostgresSqlCtlRepositoryFactory(CtlRepositorySettings settings) : IServiceFactory<ICtlRepository> {
  public ICtlRepository GetService() => new PostgresSqlCtlRepository(Getdb, settings.CreateSchema);

  private AbstractCtlRepositoryDbContext Getdb() => new PostgresSqlCtlRepositoryDbContext(settings);
}

public class PostgresSqlCtlRepository(Func<AbstractCtlRepositoryDbContext> getdb, bool createschema) : AbstractEFCtlRepository(getdb) {
  
  public override async Task<ICtlRepository> Initialise() {
    if (!createschema) return this;
    
    return await UseDb(async db => {
      await CreateSchema(db);
      return this;
    });
  }

}