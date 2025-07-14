using Centazio.Core;
using Centazio.Core.Ctl;
using Centazio.Core.Settings;
using Centazio.Providers.EF;

namespace Centazio.Providers.Sqlite.Ctl;

public class SqliteCtlRepositoryFactory(CtlRepositorySettings settings) : IServiceFactory<ICtlRepository> {
  public ICtlRepository GetService() => new SqliteCtlRepository(Getdb, settings.CreateSchema);

  private AbstractCtlRepositoryDbContext Getdb() => new SqliteCtlRepositoryDbContext(settings);
}

public class SqliteCtlRepository(Func<AbstractCtlRepositoryDbContext> getdb, bool createschema) : AbstractEFCtlRepository(getdb) {
  
  public override async Task<ICtlRepository> Initialise() {
    if (!createschema) return this;
    
    return await UseDb(async db => {
      await CreateSchema(db);
      return this;
    });
  }
}