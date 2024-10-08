using Centazio.Core;
using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.EntitySysMapping;
using Centazio.Core.Promote;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Core.Stage;
using Centazio.Core.Write;
using Centazio.Test.Lib.CoreStorage;

namespace Centazio.Test.Lib;

public static class TestingFactories {
    
  public static TestingStagedEntityStore SeStore() => new(); 
  public static TestingCtlRepository CtlRepo() => new();
  public static TestingInMemoryCoreStorageRepository CoreRepo() => new();
  public static TestingInMemoryCoreToSystemMapStore CoreSysMap() => new();
  public static IOperationRunner<ReadOperationConfig, ReadOperationResult> ReadRunner(IStagedEntityStore? store = null) => new ReadOperationRunner(store ?? SeStore());
  public static IOperationRunner<PromoteOperationConfig, PromoteOperationResult> PromoteRunner(
      IStagedEntityStore? store = null, 
      ICoreToSystemMapStore? entitymap = null, 
      ICoreStorage? core = null) => 
      new PromoteOperationRunner(store ?? SeStore(), core ?? CoreRepo(), entitymap ?? CoreSysMap());

  public static CoreEntity NewCoreCust(string first, string last, string? id = null) {
    id ??= Guid.NewGuid().ToString();
    var dob = DateOnly.MinValue;
    return new CoreEntity(id, first, last, dob, UtcDate.UtcNow);
  }

  public static WriteOperationRunner<C> WriteRunner<C>(TestingInMemoryCoreToSystemMapStore? entitymap = null, TestingInMemoryCoreStorageRepository? core = null) where C : WriteOperationConfig  
      => new(entitymap ?? CoreSysMap(), core ?? CoreRepo());

}

public class TestingStagedEntityStore() : InMemoryStagedEntityStore(0, Helpers.TestingStagedEntityChecksum) { public List<StagedEntity> Contents => saved.ToList(); }

public class TestingCtlRepository : InMemoryCtlRepository {
  public Dictionary<(SystemName, LifecycleStage), SystemState> Systems => systems;
  public Dictionary<(SystemName, LifecycleStage, ObjectName), ObjectState> Objects => objects;
}

public interface ITestingInMemoryCoreToSystemMapStore : ICoreToSystemMapStore { Task<List<Map.CoreToSystem>> GetAll(); }

public class TestingInMemoryCoreToSystemMapStore : InMemoryCoreToSystemMapStore, ITestingInMemoryCoreToSystemMapStore {
  public Task<List<Map.CoreToSystem>> GetAll() => Task.FromResult(memdb.Values.ToList()); 
}