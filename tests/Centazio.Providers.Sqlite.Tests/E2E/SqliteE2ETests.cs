using Centazio.Core.Ctl;
using Centazio.Core.Stage;
using Centazio.Providers.EF;
using Centazio.Providers.Sqlite.Ctl;
using Centazio.Providers.Sqlite.Stage;
using Centazio.Test.Lib.E2E;
using Microsoft.Data.Sqlite;

namespace Centazio.Providers.Sqlite.Tests.E2E;

public class E2E {
  [Test] public async Task Run_e2e_simulation_and_tests() {
    await new E2EEnvironment(new SqliteSimulationProvider()).RunSimulation();
  }
}

public class SqliteSimulationProvider : ISimulationProvider {

  private const string SIM_SQLITE_FILENAME = "centazio_simulation.db";
  private SqliteConnection sqliteconn => new($"Data Source={SIM_SQLITE_FILENAME};");
  
  public ICtlRepository CtlRepo { get; private set; } = null!;
  public IStagedEntityRepository StageRepository { get; private set; } = null!;
  public ISimulationCoreStorage CoreStore { get; private set; } = null!;
  
  public async Task Initialise(SimulationCtx ctx) {
    DapperInitialiser.Initialise();
    
    File.Delete(SIM_SQLITE_FILENAME);
    
    CtlRepo = await new EFCoreCtlRepository(() => new SqliteCtlContext()).Initalise();
    StageRepository = await new EFCoreStagedEntityRepository(new EFCoreStagedEntityRepositoryOptions(0, ctx.ChecksumAlg.Checksum, () => new SqliteStagedEntityContext())).Initialise();
    CoreStore = await new SqliteCoreStorage(ctx, () => sqliteconn).Initialise();
  }
  
  public async ValueTask DisposeAsync() {
    await CoreStore.DisposeAsync();
    await StageRepository.DisposeAsync();
    await CtlRepo.DisposeAsync();
  }
}