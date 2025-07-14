using Centazio.Core.Ctl;
using Centazio.Core.Stage;
using Centazio.Providers.EF;
using Centazio.Providers.EF.Tests;
using Centazio.Providers.EF.Tests.E2E;
using Centazio.Providers.SqlServer.Ctl;
using Centazio.Providers.SqlServer.Stage;
using Centazio.Test.Lib.E2E;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.SqlServer.Tests.E2E;

public class SqlServerE2ETests : BaseE2ETests {
  protected override Task<ISimulationStorage> GetStorage() => Task.FromResult<ISimulationStorage>(new SqlServerSimulationStorage());
}

public class SqlServerSimulationStorage : ISimulationStorage {
  public ICtlRepository CtlRepo { get; private set; } = null!;
  public IStagedEntityRepository StageRepository { get; private set; } = null!;
  public ISimulationCoreStorageRepository CoreStore { get; private set; } = null!;
  public int SimulationPostFunctionRunDelayMs => 500;

  public async Task Initialise(SimulationCtx ctx) {
    var connstr = (await SqlConn.GetInstance(false, await F.Secrets())).ConnStr;
    var settings = await F.Settings();
    var ctlsetts = settings.CtlRepository with { ConnectionString = connstr };
    var stgsetts = settings.StagedEntityRepository with { ConnectionString = connstr };
    CtlRepo = await new TestingEfCtlSimulationRepository(ctx.Epoch, () => new SqlServerCtlRepositoryDbContext(ctlsetts)).Initialise();
    StageRepository = await new TestingEfStagedEntityRepository(
        new EFStagedEntityRepositoryOptions(0, ctx.ChecksumAlg.Checksum, () => new SqlServerStagedEntityContext(stgsetts))).Initialise();
    CoreStore = await new SimulationEfCoreStorageRepository(
        () => new SimulationSqlServerDbContext(connstr), 
        ctx.Epoch).Initialise();
  }
  
  public async ValueTask DisposeAsync() {
    await CoreStore.DisposeAsync();
    await StageRepository.DisposeAsync();
    await CtlRepo.DisposeAsync();
  }
}

public class SimulationSqlServerDbContext(string connstr) : SqlServerDbContext(connstr) {
  protected override void CreateCentazioModel(ModelBuilder builder) {
    SimulationEfCoreStorageRepository.CreateSimulationCoreStorageEfModel(builder);
  }
}