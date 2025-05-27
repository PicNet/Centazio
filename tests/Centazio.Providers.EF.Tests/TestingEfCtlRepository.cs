using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Centazio.Test.Lib;
using Centazio.Test.Lib.E2E;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.EF.Tests;

public class TestingEfCtlRepository(Func<AbstractCtlRepositoryDbContext> getdb, IDbFieldsHelper dbf) : AbstractEFCtlRepository(getdb), ITestingCtlRepository {

  public async Task<List<Map.CoreToSysMap>> GetAllMaps() {
    await using var db = Db();
    return (await db.CoreToSystemMaps.ToListAsync()).Select(dto => dto.ToBase()).ToList();
  }
  
  public override async Task<ICtlRepository> Initialise() {
    await using var db = Db();
    await DropTablesImpl(dbf, db);
    await CreateSchema(dbf, db);
    return await base.Initialise();
  }
  
  public override async ValueTask DisposeAsync() {
    await using var db = Db();
    await DropTablesImpl(dbf, db);
  }
}

public class TestingEfCtlSimulationRepository(EpochTracker epoch, Func<AbstractCtlRepositoryDbContext> getdb, IDbFieldsHelper dbf) : TestingEfCtlRepository(getdb, dbf) {
  
  
  protected override Task<List<EntityChange>> SaveEntityChangesImpl(List<EntityChange> batch) {
    epoch.EntityChangesUpdated(batch);
    return base.SaveEntityChangesImpl(batch);
  }
}