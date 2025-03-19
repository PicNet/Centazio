using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Stage;
using Centazio.Providers.EF;
using Centazio.Providers.EF.Tests;
using Centazio.Providers.EF.Tests.E2E;
using Centazio.Providers.Sqlite.Ctl;
using Centazio.Providers.Sqlite.Stage;
using Centazio.Test.Lib;
using Centazio.Test.Lib.E2E;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.Sqlite.Tests.E2E;

// todo: SQLite is deadlocking
public class SqliteE2ETests {
  [Test] public async Task Run_e2e_simulation_and_tests() => 
      await new E2EEnvironment(true, new SqliteSimulationProvider(), TestingFactories.Settings()).RunSimulation();

}

public class SqliteSimulationProvider : ISimulationProvider {
  public ICtlRepository CtlRepo { get; private set; } = null!;
  public IStagedEntityRepository StageRepository { get; private set; } = null!;
  public ISimulationCoreStorageRepository CoreStore { get; private set; } = null!;
  
  public async Task Initialise(SimulationCtx ctx) {
    var dbf = new SqliteDbFieldsHelper();
    CtlRepo = await new TestingEfCtlRepository(() => new SqliteCtlRepositoryDbContext(
        SqliteTestConstants.DEFAULT_CONNSTR,
        nameof(Core.Ctl).ToLower(), 
        nameof(SystemState).ToLower(), 
        nameof(ObjectState).ToLower(), 
        nameof(Map.CoreToSysMap).ToLower()), dbf).Initialise();
    StageRepository = await new TestingEfStagedEntityRepository(new EFStagedEntityRepositoryOptions(0, ctx.ChecksumAlg.Checksum, () => new SqliteStagedEntityContext(SqliteTestConstants.DEFAULT_CONNSTR)), dbf).Initialise();
    CoreStore = await new SimulationEfCoreStorageRepository(
        () => new SqliteSimulationDbContext(), 
        ctx.Epoch, dbf).Initialise();
  }
  
  public async ValueTask DisposeAsync() {
    await CoreStore.DisposeAsync();
    await StageRepository.DisposeAsync();
    await CtlRepo.DisposeAsync();
  }
}

public class SqliteSimulationDbContext() : SqliteDbContext(SqliteTestConstants.DEFAULT_CONNSTR) {

  protected override void CreateCentazioModel(ModelBuilder builder) {
    SimulationEfCoreStorageRepository.CreateSimulationCoreStorageEfModel(builder);
  }

}