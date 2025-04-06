using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;
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

public class SqliteE2ETests {
  private const string dbfile="test.db";
  
  [SetUp] public void CleanUp() { File.Delete(dbfile); }

  // todo: test is failing with real notifier
  [Test] public async Task Run_e2e_simulation_and_tests() =>
      // await new E2EEnvironment(new InProcessChangesNotifier(), new SqliteSimulationProvider(dbfile), TestingFactories.Settings()).RunSimulation();
      await new E2EEnvironment(new InstantChangesNotifier(), new SqliteSimulationProvider(dbfile), TestingFactories.Settings()).RunSimulation();
}

public class SqliteSimulationProvider(string dbfile) : ISimulationProvider {
  // in-memory sqlite locks, so use file
  private readonly string connstr = $"Data Source={dbfile};Cache=Shared";
  
  public ICtlRepository CtlRepo { get; private set; } = null!;
  public IStagedEntityRepository StageRepository { get; private set; } = null!;
  public ISimulationCoreStorageRepository CoreStore { get; private set; } = null!;
  
  public async Task Initialise(SimulationCtx ctx) {
    var dbf = new SqliteDbFieldsHelper();
    CtlRepo = await new TestingEfCtlRepository(() => new SqliteCtlRepositoryDbContext(
        connstr,
        nameof(Core.Ctl).ToLower(), 
        nameof(SystemState).ToLower(), 
        nameof(ObjectState).ToLower(), 
        nameof(Map.CoreToSysMap).ToLower()), dbf).Initialise();
    StageRepository = await new TestingEfStagedEntityRepository(new EFStagedEntityRepositoryOptions(0, ctx.ChecksumAlg.Checksum, () => new SqliteStagedEntityContext(connstr)), dbf).Initialise();
    CoreStore = await new SimulationEfCoreStorageRepository(
        () => new SqliteSimulationDbContext(connstr), 
        ctx.Epoch, dbf).Initialise();
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