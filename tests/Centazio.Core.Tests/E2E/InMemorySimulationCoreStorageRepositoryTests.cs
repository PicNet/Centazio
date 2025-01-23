using Centazio.Test.Lib;
using Centazio.Test.Lib.BaseProviderTests;
using Centazio.Test.Lib.E2E;
using Centazio.Test.Lib.InMemRepos;

namespace Centazio.Core.Tests.E2E;

public class SimulationCoreStorageRepositoryTests : BaseSimulationCoreStorageRepositoryTests {

  protected override Task<ISimulationCoreStorageRepository> GetRepository(InMemoryEpochTracker tracker) => 
      Task.FromResult<ISimulationCoreStorageRepository>(new InMemorySimulationCoreStorageRepository(tracker, Helpers.TestingCoreEntityChecksum));
}

