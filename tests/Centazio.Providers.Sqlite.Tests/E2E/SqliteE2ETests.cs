using Centazio.Core.Ctl;
using Centazio.Core.Stage;
using Centazio.Providers.EF;
using Centazio.Providers.EF.Tests;
using Centazio.Providers.EF.Tests.E2E;
using Centazio.Providers.Sqlite.Ctl;
using Centazio.Providers.Sqlite.Stage;
using Centazio.Test.Lib.E2E;
using Microsoft.Data.Sqlite;
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
  public async Task Initialise(SimulationCtx ctx) {
    var (ctl_db, staging_db, core_db) = (GetNewDbFileConnStr("ctl"), GetNewDbFileConnStr("staging"), GetNewDbFileConnStr("core"));
    
    var settings = await F.Settings(); 
    var ctlsetts = settings.CtlRepository with { ConnectionString = ctl_db };
    var stgsetts = settings.StagedEntityRepository with { ConnectionString = staging_db };
    CtlRepo = await new TestingEfCtlSimulationRepository(ctx.Epoch, () => new SqliteCtlRepositoryDbContext(ctlsetts), ctx.Clock).Initialise();
    StageRepository = await new EfStagedEntityRepository(new EfStagedEntityRepositoryOptions(0, ctx.ChecksumAlg.Checksum, () => new SqliteStagedEntityContext(stgsetts), ctx.Clock, ctx.Ids), true).Initialise();
    CoreStore = await new SimulationEfCoreStorageRepository(() => new SqliteSimulationDbContext(core_db), ctx.Epoch).Initialise();
    
    string GetNewDbFileConnStr(string repo) {
      var dbfile = $"{TestContext.CurrentContext.Test.Name}_{repo}.db";
      // a test can run several simulations, pooled (shared cache) connections would still point at the deleted file
      SqliteConnection.ClearAllPools();
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