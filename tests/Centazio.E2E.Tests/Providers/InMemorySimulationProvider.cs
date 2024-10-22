using Centazio.Core.Ctl;
using Centazio.Core.Stage;
using Centazio.E2E.Tests.Infra;
using Centazio.Test.Lib.InMemRepos;

namespace Centazio.E2E.Tests.Providers;

public class InMemorySimulationProvider : ISimulationProvider {

  public ICtlRepository CtlRepo { get; private set; } = null!;
  public IStagedEntityRepository StageRepository { get; private set; } = null!;
  public ISimulationCoreStorage CoreStore { get; private set; } = null!;
  
  public Task Initialise(SimulationCtx ctx) {
    CtlRepo = new InMemoryCtlRepository();
    StageRepository = new InMemoryStagedEntityRepository(0, ctx.ChecksumAlg.Checksum);
    CoreStore = new InMemoryCoreStorage(ctx);
    
    return Task.CompletedTask;
  }
  
  public async ValueTask DisposeAsync() {
    await CoreStore.DisposeAsync();
    await StageRepository.DisposeAsync();
    await CtlRepo.DisposeAsync();
  }
}