﻿using Centazio.Core.Ctl;
using Centazio.Core.Stage;
using Centazio.Providers.EF;
using Centazio.Providers.EF.Tests;
using Centazio.Providers.EF.Tests.E2E;
using Centazio.Providers.SqlServer.Ctl;
using Centazio.Providers.SqlServer.Stage;
using Centazio.Test.Lib.E2E;

namespace Centazio.Providers.SqlServer.Tests.E2E;

public class SqlServerE2ETests {
  [Test] public async Task Run_e2e_simulation_and_tests() {
    await new E2EEnvironment(new SqlServerSimulationProvider()).RunSimulation();
  }
}

public class SqlServerSimulationProvider : ISimulationProvider {
  public ICtlRepository CtlRepo { get; private set; } = null!;
  public IStagedEntityRepository StageRepository { get; private set; } = null!;
  public ISimulationCoreStorageRepository CoreStore { get; private set; } = null!;
  
  public async Task Initialise(SimulationCtx ctx) {
    DapperInitialiser.Initialise();
    
    var dbf = new SqlServerDbFieldsHelper();
    var connstr = await SqlConn.Instance.ConnStr();
    CtlRepo = await new TestingEfCtlRepository(() => new SqlServerCtlContext(connstr), dbf).Initalise();
    StageRepository = await new TestingEfStagedEntityRepository(new EFStagedEntityRepositoryOptions(0, ctx.ChecksumAlg.Checksum, () => new SqlServerStagedEntityContext(connstr)), dbf).Initialise();
    CoreStore = await new SimulationEfCoreStorageRepository(() => new SqlServerCoreStorageDbContext(connstr), ctx.Epoch, ctx.ChecksumAlg.Checksum, dbf).Initialise();
  }
  
  public async ValueTask DisposeAsync() {
    await CoreStore.DisposeAsync();
    await StageRepository.DisposeAsync();
    await CtlRepo.DisposeAsync();
  }
}