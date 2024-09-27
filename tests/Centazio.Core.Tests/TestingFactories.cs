using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.EntitySysMapping;
using Centazio.Core.Promote;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Core.Stage;
using Centazio.Core.Tests.CoreRepo;
using Centazio.Core.Tests.IntegrationTests;
using Centazio.Core.Write;

namespace Centazio.Core.Tests;

public static class TestingFactories {
    
  public static TestingStagedEntityStore SeStore() => new(); 
  public static TestingCtlRepository CtlRepo() => new();
  public static TestingInMemoryCoreStorageRepository CoreRepo() => new();
  public static InMemoryEntityIntraSystemMappingStore EntitySysMap() => new();
  public static IOperationRunner<ReadOperationConfig, ExternalEntityType, ReadOperationResult> ReadRunner(IStagedEntityStore? store = null) => new ReadOperationRunner(store ?? SeStore());
  public static IOperationRunner<PromoteOperationConfig, CoreEntityType, PromoteOperationResult> PromoteRunner(
      IStagedEntityStore? store = null, 
      IEntityIntraSystemMappingStore? entitymap = null, 
      ICoreStorageUpserter? core = null) => 
      new PromoteOperationRunner(store ?? SeStore(), entitymap ?? EntitySysMap(), core ?? CoreRepo());

  public static CoreEntity NewCoreCust(string first, string last, string? id = null, string? checksum = null) {
    id ??= Guid.NewGuid().ToString();
    var dob = DateOnly.MinValue;
    checksum ??= Test.Lib.Helpers.TestingChecksum(new { id, first, last, dob });
    return new CoreEntity(id, checksum, first, last, dob, UtcDate.UtcNow);
  }

  public static WriteOperationRunner<C> WriteRunner<C>(InMemoryEntityIntraSystemMappingStore? entitymap = null, TestingInMemoryCoreStorageRepository? core = null) where C : WriteOperationConfig  
      => new(entitymap ?? EntitySysMap(), core ?? CoreRepo());

}

public class TestingStagedEntityStore() : InMemoryStagedEntityStore(0, Test.Lib.Helpers.TestingChecksum) { public List<StagedEntity> Contents => saved.ToList(); }

public class TestingCtlRepository : InMemoryCtlRepository {
  public Dictionary<(SystemName, LifecycleStage), SystemState> Systems => systems;
  public Dictionary<(SystemName, LifecycleStage, O), ObjectState<O>> GetObjects<O>() where O : ObjectName=> ((InMemoryObjectStateRepository<O>)GetObjectStateRepo<O>()).objects;
}