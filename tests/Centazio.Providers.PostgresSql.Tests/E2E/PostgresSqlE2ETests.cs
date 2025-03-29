using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
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

public class PostgresSqlE2ETests {
  
  [Test] public async Task Run_e2e_simulation_and_tests() {
    var connstr = await new PostgresSqlConnection().Init();
    await new E2EEnvironment(true, new PostgresSqlSimulationProvider(connstr), TestingFactories.Settings()).RunSimulation();
  }

}

public class PostgresSqlSimulationProvider(string connstr) : ISimulationProvider {
  
  public ICtlRepository CtlRepo { get; private set; } = null!;
  public IStagedEntityRepository StageRepository { get; private set; } = null!;
  public ISimulationCoreStorageRepository CoreStore { get; private set; } = null!;
  
  public async Task Initialise(SimulationCtx ctx) {
    var dbf = new PostgresSqlDbFieldsHelper();
    CtlRepo = await new TestingEfCtlRepository(() => new PostgresSqlCtlRepositoryDbContext(
        connstr,
        nameof(Core.Ctl).ToLower(), 
        nameof(SystemState).ToLower(), 
        nameof(ObjectState).ToLower(), 
        nameof(Map.CoreToSysMap).ToLower()), dbf).Initialise();
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