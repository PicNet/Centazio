﻿using Centazio.Core.Ctl;
using Centazio.Core.Stage;
using Centazio.Providers.EF;
using Centazio.Providers.EF.Tests;
using Centazio.Providers.EF.Tests.E2E;
using Centazio.Providers.Sqlite.Ctl;
using Centazio.Providers.Sqlite.Stage;
using Centazio.Test.Lib.E2E;

namespace Centazio.Providers.Sqlite.Tests.E2E;

public class SqliteE2ETests {
  [Test] public async Task Run_e2e_simulation_and_tests() {
    await new E2EEnvironment(new SqliteSimulationProvider()).RunSimulation();
  }
}

public class SqliteSimulationProvider : ISimulationProvider {
  public ICtlRepository CtlRepo { get; private set; } = null!;
  public IStagedEntityRepository StageRepository { get; private set; } = null!;
  public ISimulationCoreStorageRepository CoreStore { get; private set; } = null!;
  
  public async Task Initialise(SimulationCtx ctx) {
    var dbf = new SqliteDbFieldsHelper();
    CtlRepo = await new TestingEfCtlRepository(() => new SqliteCtlContext(), dbf).Initalise();
    StageRepository = await new TestingEfStagedEntityRepository(new EFStagedEntityRepositoryOptions(0, ctx.ChecksumAlg.Checksum, () => new SqliteStagedEntityContext()), dbf).Initialise();
    CoreStore = await new SimulationEfCoreStorageRepository(() => new SqliteSimulationCoreStorageDbContext(), ctx.Epoch, ctx.ChecksumAlg.Checksum, dbf).Initialise();
  }
  
  public async ValueTask DisposeAsync() {
    await CoreStore.DisposeAsync();
    await StageRepository.DisposeAsync();
    await CtlRepo.DisposeAsync();
  }
}