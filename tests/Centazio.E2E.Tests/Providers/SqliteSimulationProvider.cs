using Centazio.Core.Ctl;
using Centazio.Core.Stage;
using Centazio.E2E.Tests.Infra;
using Centazio.Providers.Sqlite;
using Centazio.Providers.Sqlite.Ctl;
using Centazio.Providers.Sqlite.Stage;
using Microsoft.Data.Sqlite;

namespace Centazio.E2E.Tests.Providers;

public class SqliteSimulationProvider : ISimulationProvider {

  private const string SIM_SQLITE_FILENAME = "centazio_simulation.db";
  private SqliteConnection sqliteconn => new($"Data Source={SIM_SQLITE_FILENAME};");
  
  public ICtlRepository CtlRepo { get; private set; } = null!;
  public IStagedEntityRepository StageRepository { get; private set; } = null!;
  public ISimulationCoreStorage CoreStore { get; private set; } = null!;
  
  public async Task Initialise(SimulationCtx ctx) {
    DapperInitialiser.Initialise();
    
    File.Delete(SIM_SQLITE_FILENAME);
    
    CtlRepo = await new SqliteCtlRepository(() => sqliteconn).Initalise();
    StageRepository = await new SqliteStagedEntityRepository(() => sqliteconn, 0, ctx.ChecksumAlg.Checksum).Initalise();
    CoreStore = new InMemoryCoreStorage(ctx);
  }
  
  public async ValueTask DisposeAsync() {
    await CoreStore.DisposeAsync();
    await StageRepository.DisposeAsync();
    await CtlRepo.DisposeAsync();
  }
}