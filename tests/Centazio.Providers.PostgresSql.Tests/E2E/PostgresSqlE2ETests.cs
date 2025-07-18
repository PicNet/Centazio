﻿using Centazio.Core.Ctl;
using Centazio.Core.Settings;
using Centazio.Core.Stage;
using Centazio.Providers.EF;
using Centazio.Providers.EF.Tests;
using Centazio.Providers.EF.Tests.E2E;
using Centazio.Providers.PostgresSql.Ctl;
using Centazio.Providers.PostgresSql.Stage;
using Centazio.Test.Lib.E2E;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.PostgresSql.Tests.E2E;

public class PostgresSqlE2ETests : BaseE2ETests {
  protected override async Task<ISimulationStorage> GetStorage() {
    var settings = await F.Settings();
    var connstr = await new PostgresSqlConnection().Init();
    var ctlsetts = settings.CtlRepository with { ConnectionString = connstr };
    var stgsetts = settings.StagedEntityRepository with { ConnectionString = connstr };
    return new PostgresSqlSimulationStorage(connstr, ctlsetts, stgsetts);
  }

}

public class PostgresSqlSimulationStorage(string connstr, CtlRepositorySettings ctlsetts, StagedEntityRepositorySettings stgsetts) : ISimulationStorage {
  
  public ICtlRepository CtlRepo { get; private set; } = null!;
  public IStagedEntityRepository StageRepository { get; private set; } = null!;
  public ISimulationCoreStorageRepository CoreStore { get; private set; } = null!;
  
  public int SimulationPostFunctionRunDelayMs => 100;

  public async Task Initialise(SimulationCtx ctx) {
    CtlRepo = await new TestingEfCtlSimulationRepository(ctx.Epoch, () => new PostgresSqlCtlRepositoryDbContext(ctlsetts)).Initialise();
    StageRepository = await new EfStagedEntityRepository(new EfStagedEntityRepositoryOptions(0, ctx.ChecksumAlg.Checksum, () => new PostgresSqlStagedEntityContext(stgsetts)), true).Initialise();
    CoreStore = await new SimulationEfCoreStorageRepository(
        () => new PostgresSqlSimulationDbContext(connstr), 
        ctx.Epoch).Initialise();
  }
  
  public async ValueTask DisposeAsync() {
    await CoreStore.DisposeAsync();
    await StageRepository.DisposeAsync();
    await CtlRepo.DisposeAsync();
  }
}

public class PostgresSqlSimulationDbContext(string connstr) : PostgresSqlDbContext(connstr) {

  protected override void CreateCentazioModel(ModelBuilder builder) => 
      SimulationEfCoreStorageRepository.CreateSimulationCoreStorageEfModel(builder);
}