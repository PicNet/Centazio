using Centazio.Core.Stage;
using Centazio.Test.Lib.E2E;
using Centazio.Test.Lib.InMemRepos;

namespace Centazio.Core.Tests.E2E;

public class InMemorySimulationStorage : ISimulationStorage {

  public ICtlRepository CtlRepo { get; private set; } = null!;
  public IStagedEntityRepository StageRepository { get; private set; } = null!;
  public ISimulationCoreStorageRepository CoreStore { get; private set; } = null!;
  
  public Task Initialise(SimulationCtx ctx) {
    CtlRepo = new InMemorySimulationCtlRepository(ctx.Epoch, ctx.Clock);
    StageRepository = new InMemoryStagedEntityRepository(0, ctx.ChecksumAlg.Checksum, ctx.Clock, ctx.Ids);
    CoreStore = new InMemorySimulationCoreStorageRepository(ctx.Epoch, ctx.ChecksumAlg.Checksum);
    
    return Task.CompletedTask;
  }
  
  public async ValueTask DisposeAsync() {
    await CoreStore.DisposeAsync();
    await StageRepository.DisposeAsync();
    await CtlRepo.DisposeAsync();
  }
}

public class InMemorySimulationCtlRepository(EpochTracker epoch, TimeProvider clock) : InMemoryBaseCtlRepository(clock) {
  protected override Task<List<EntityChange>> SaveEntityChangesImpl(List<EntityChange> batch) {
    epoch.EntityChangesUpdated(batch);
    return base.SaveEntityChangesImpl(batch);
  }
}