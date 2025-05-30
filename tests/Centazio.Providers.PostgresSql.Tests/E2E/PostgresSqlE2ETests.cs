using Centazio.Core.Ctl;
using Centazio.Core.Settings;
using Centazio.Core.Stage;
using Centazio.Providers.EF;
using Centazio.Providers.EF.Tests;
using Centazio.Providers.EF.Tests.E2E;
using Centazio.Providers.PostgresSql.Ctl;
using Centazio.Providers.PostgresSql.Stage;
using Centazio.Test.Lib;
using Centazio.Test.Lib.E2E;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Providers.PostgresSql.Tests.E2E;

public class PostgresSqlE2ETests : BaseE2ETests {
  private string? connstr;
  protected override async Task<ISimulationStorage> GetStorage() {
    var settings = (await TestingFactories.Settings()).CtlRepository;
    return new PostgresSqlSimulationStorage(connstr ??= await new PostgresSqlConnection().Init(), settings);
  }

}

public class PostgresSqlSimulationStorage(string connstr, CtlRepositorySettings settings) : ISimulationStorage {
  
  public ICtlRepository CtlRepo { get; private set; } = null!;
  public IStagedEntityRepository StageRepository { get; private set; } = null!;
  public ISimulationCoreStorageRepository CoreStore { get; private set; } = null!;
  
  public int PostEpochDelayMs => 250;

  public async Task Initialise(SimulationCtx ctx) {
    var dbf = new PostgresSqlDbFieldsHelper();
    // todo: can we just pass in the CtlSettings here instead of all table names?
    // todo: ensure that StageEntityRepo also allows setting the table names using same pattern
    CtlRepo = await new TestingEfCtlSimulationRepository(ctx.Epoch, () => new PostgresSqlCtlRepositoryDbContext(settings), dbf).Initialise();
    StageRepository = await new TestingEfStagedEntityRepository(new EFStagedEntityRepositoryOptions(0, ctx.ChecksumAlg.Checksum, () => new PostgresSqlStagedEntityContext(connstr)), dbf).Initialise();
    CoreStore = await new SimulationEfCoreStorageRepository(
        () => new PostgresSqlSimulationDbContext(connstr), 
        ctx.Epoch, dbf).Initialise();
  }
  
  public async ValueTask DisposeAsync() {
    await CoreStore.DisposeAsync();
    await StageRepository.DisposeAsync();
    await CtlRepo.DisposeAsync();
  }
}

public class PostgresSqlSimulationDbContext(string connstr) : PostgresSqlDbContext(connstr) {

  protected override void CreateCentazioModel(ModelBuilder builder) {
    SimulationEfCoreStorageRepository.CreateSimulationCoreStorageEfModel(builder);
  }

}