using Centazio.Core;
using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Promote;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Core.Stage;
using Centazio.Core.Write;
using Centazio.Test.Lib.InMemRepos;

namespace Centazio.Test.Lib;

public static class TestingFactories {
    
  public static TestingStagedEntityRepository SeRepo() => new(); 
  public static TestingInMemoryBaseCtlRepository CtlRepo() => new();
  public static TestingInMemoryCoreStorageRepository CoreRepo() => new();
  public static IOperationRunner<ReadOperationConfig, ReadOperationResult> ReadRunner(IStagedEntityRepository? serepo = null) => new ReadOperationRunner(serepo ?? SeRepo());
  public static IOperationRunner<PromoteOperationConfig, PromoteOperationResult> PromoteRunner(
      IStagedEntityRepository? serepo = null, 
      ICtlRepository? ctl = null, 
      ICoreStorage? core = null) => 
      new PromoteOperationRunner(serepo ?? SeRepo(), core ?? CoreRepo(), ctl ?? CtlRepo());

  public static CoreEntity NewCoreCust(string first, string last, CoreEntityId? id = null) {
    id ??= new(Guid.NewGuid().ToString());
    var dob = DateOnly.MinValue;
    return new CoreEntity(id, first, last, dob) { DateCreated = UtcDate.UtcNow, DateUpdated = UtcDate.UtcNow };
  }

  public static WriteOperationRunner<C> WriteRunner<C>(TestingInMemoryBaseCtlRepository? ctl = null, TestingInMemoryCoreStorageRepository? core = null) where C : WriteOperationConfig  
      => new(ctl ?? CtlRepo(), core ?? CoreRepo());

}

public class TestingStagedEntityRepository() : InMemoryStagedEntityRepository(0, Helpers.TestingStagedEntityChecksum) { public List<StagedEntity> Contents => saved.ToList(); }

public class TestingInMemoryBaseCtlRepository : InMemoryBaseCtlRepository, ITestingCtlRepository {
  public Dictionary<(SystemName, LifecycleStage), SystemState> Systems => systems;
  public Dictionary<(SystemName, LifecycleStage, ObjectName), ObjectState> Objects => objects;
  public Dictionary<Map.Key, string> Maps => maps;
  
  public Task<List<Map.CoreToSysMap>> GetAllMaps() => 
      Task.FromResult(maps.Values.Select(Deserialize).Cast<Map.CoreToSysMap>().ToList());
}