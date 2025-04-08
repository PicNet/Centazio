﻿using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Stage;
using Centazio.Providers.EF;
using Centazio.Providers.EF.Tests;
using Centazio.Providers.EF.Tests.E2E;
using Centazio.Providers.Sqlite.Ctl;
using Centazio.Providers.Sqlite.Stage;
using Centazio.Test.Lib.E2E;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.Sqlite.Tests.E2E;

public class SqliteE2ETests : BaseE2ETests {
  protected override Task<ISimulationStorage> GetStorage() => 
      Task.FromResult<ISimulationStorage>(new SqliteSimulationStorage());

}

public class SqliteSimulationStorage : ISimulationStorage {
  
  public ICtlRepository CtlRepo { get; private set; } = null!;
  public IStagedEntityRepository StageRepository { get; private set; } = null!;
  public ISimulationCoreStorageRepository CoreStore { get; private set; } = null!;
  
  // sqlite is terrible with multithreading and needs a delay during the simulation when using an async notifier
  public int PostEpochDelayMs => 750;  

  public async Task Initialise(SimulationCtx ctx) {
    var (ctl_db, staging_db, core_db) = (GetNewDbFileConnStr("ctl"), GetNewDbFileConnStr("staging"), GetNewDbFileConnStr("core"));  
    var dbf = new SqliteDbFieldsHelper();
    CtlRepo = await new TestingEfCtlRepository(() => new SqliteCtlRepositoryDbContext(
        ctl_db,
        nameof(Core.Ctl).ToLower(), 
        nameof(SystemState).ToLower(), 
        nameof(ObjectState).ToLower(), 
        nameof(Map.CoreToSysMap).ToLower()), dbf).Initialise();
    StageRepository = await new TestingEfStagedEntityRepository(new EFStagedEntityRepositoryOptions(0, ctx.ChecksumAlg.Checksum, () => new SqliteStagedEntityContext(staging_db)), dbf).Initialise();
    CoreStore = await new SimulationEfCoreStorageRepository(
        () => new SqliteSimulationDbContext(core_db), 
        ctx.Epoch, dbf).Initialise();
    
    string GetNewDbFileConnStr(string repo) {
      var dbfile = $"{TestContext.CurrentContext.Test.Name}_{repo}.db";
      if (File.Exists(dbfile)) File.Delete(dbfile);
      return $"Data Source={dbfile};Cache=Shared;";
    }
  }

  public async ValueTask DisposeAsync() {
    await CoreStore.DisposeAsync();
    await StageRepository.DisposeAsync();
    await CtlRepo.DisposeAsync();
  }
}

public class SqliteSimulationDbContext(string connstr) : SqliteDbContext(connstr) {

  protected override void CreateCentazioModel(ModelBuilder builder) {
    SimulationEfCoreStorageRepository.CreateSimulationCoreStorageEfModel(builder);
  }

}