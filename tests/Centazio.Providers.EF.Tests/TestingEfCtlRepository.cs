using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Test.Lib;
using Centazio.Test.Lib.E2E;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.EF.Tests;

public class TestingEfCtlRepository(Func<AbstractCtlRepositoryDbContext> getdb, TimeProvider clock) : AbstractEFCtlRepository(getdb, clock), ITestingCtlRepository {

  public async Task<List<Map.CoreToSysMap>> GetAllMaps() => 
      await UseDb(async db => 
          (await db.CoreToSystemMaps.ToListAsync()).Select(dto => dto.ToBase()).ToList());

  public override async Task<ICtlRepository> Initialise() => 
      await UseDb(async db => {
        await DropTablesImpl(db);
        await CreateSchema(db);
        return await base.Initialise();
      });

  public override async ValueTask DisposeAsync() => 
      await UseDb(async db => {
        await DropTablesImpl(db);
        return Task.CompletedTask;
      });

}

public class TestingEfCtlSimulationRepository(EpochTracker epoch, Func<AbstractCtlRepositoryDbContext> getdb, TimeProvider clock) : TestingEfCtlRepository(getdb, clock) {
  
  
  protected override Task<List<EntityChange>> SaveEntityChangesImpl(List<EntityChange> batch) {
    epoch.EntityChangesUpdated(batch);
    return base.SaveEntityChangesImpl(batch);
  }
}