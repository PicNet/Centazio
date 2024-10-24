using Centazio.Core.Ctl;
using Centazio.Core.Stage;
using Centazio.Providers.EF;
using Centazio.Providers.Sqlite.Ctl;
using Centazio.Providers.Sqlite.Stage;
using Centazio.Test.Lib.E2E;

namespace Centazio.Providers.Sqlite.Tests.E2E;

public class E2E {
  [Test] public async Task Run_e2e_simulation_and_tests() {
    await new E2EEnvironment(new SqliteSimulationProvider()).RunSimulation();
  }
}

public class SqliteSimulationProvider : ISimulationProvider {
  public ICtlRepository CtlRepo { get; private set; } = null!;
  public IStagedEntityRepository StageRepository { get; private set; } = null!;
  public ISimulationCoreStorage CoreStore { get; private set; } = null!;
  
  public async Task Initialise(SimulationCtx ctx) {
    DapperInitialiser.Initialise();
    
    CtlRepo = await new EFCoreCtlRepository(() => new SqliteCtlContext()).Initalise();
    StageRepository = await new EFCoreStagedEntityRepository(new EFCoreStagedEntityRepositoryOptions(0, ctx.ChecksumAlg.Checksum, () => new SqliteStagedEntityContext())).Initialise();
    CoreStore = await new EFCoreStorage(ctx, () => new SqliteCoreStorageDbContext()).Initialise();
  }
  
  public async ValueTask DisposeAsync() {
    await CoreStore.DisposeAsync();
    await StageRepository.DisposeAsync();
    await CtlRepo.DisposeAsync();
  }
}