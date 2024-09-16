using System.Text.Json;
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

namespace Centazio.Core.Tests;

public static class TestingFactories {
    
  public static TestingStagedEntityStore SeStore() => new(); 
  public static TestingCtlRepository CtlRepo() => new();
  public static TestingInMemoryCoreStorageRepository CoreRepo() => new();
  public static InMemoryEntityIntraSystemMappingStore EntitySysMap() => new();
  public static IOperationRunner<ReadOperationConfig, ReadOperationResult> ReadRunner(IStagedEntityStore? store = null) => new ReadOperationRunner(store ?? SeStore());
  public static IOperationRunner<PromoteOperationConfig<CoreCustomer>, PromoteOperationResult<CoreCustomer>> PromoteRunner(
      IStagedEntityStore? store = null, 
      IEntityIntraSystemMappingStore? entitymap = null, 
      ICoreStorageUpserter? core = null) => 
      new PromoteOperationRunner<CoreCustomer>(store ?? SeStore(), entitymap ?? EntitySysMap(), core ?? CoreRepo());
  
  public static Task<ReadOperationResult> TestingAbortingAndEmptyReadOperationImplementation(OperationStateAndConfig<ReadOperationConfig> op) {
    var result = Enum.Parse<EOperationResult>(op.Settings.Object);
    ReadOperationResult res = result == EOperationResult.Error ? new ErrorReadOperationResult(EOperationAbortVote.Abort) : new EmptyReadOperationResult(); 
    return Task.FromResult(res);
  }
  
  public static Task<ReadOperationResult> TestingEmptyReadOperationImplementation(OperationStateAndConfig<ReadOperationConfig> op) {
    var result = Enum.Parse<EOperationResult>(op.Settings.Object);
    ReadOperationResult res = result == EOperationResult.Error ? new ErrorReadOperationResult() : new EmptyReadOperationResult();
    return Task.FromResult(res);
  }

  public static Task<ReadOperationResult> TestingSingleReadOperationImplementation(OperationStateAndConfig<ReadOperationConfig> op) {
    var result = Enum.Parse<EOperationResult>(op.Settings.Object); 
    ReadOperationResult res = result == EOperationResult.Error ? new ErrorReadOperationResult() : new SingleRecordReadOperationResult(Guid.NewGuid().ToString());
    return Task.FromResult(res);
  }

  public static Task<ReadOperationResult> TestingListReadOperationImplementation(OperationStateAndConfig<ReadOperationConfig> op) {
    var result = Enum.Parse<EOperationResult>(op.Settings.Object); 
    ReadOperationResult res = result == EOperationResult.Error ? new ErrorReadOperationResult() : new ListRecordsReadOperationResult(Enumerable.Range(0, 100).Select(_ => Guid.NewGuid().ToString()).ToList());
    return Task.FromResult(res); 
  }
  
  public static string TestingChecksum(string data) => data.GetHashCode().ToString(); // simple fast
  public static string TestingChecksum(object obj) => TestingChecksum(JsonSerializer.Serialize(obj));

  public static CoreCustomer NewCoreCust(string first, string last, string? id = null, string? checksum = null) {
    id ??= Guid.NewGuid().ToString();
    var dob = DateOnly.MinValue;
    checksum ??= TestingChecksum(new { id, first, last, dob });
    return new CoreCustomer(id, checksum, first, last, dob, UtcDate.UtcNow);
  }
}

public class TestingStagedEntityStore() : InMemoryStagedEntityStore(0, TestingFactories.TestingChecksum) { public List<StagedEntity> Contents => saved.ToList(); }

public class TestingCtlRepository : InMemoryCtlRepository {
  public Dictionary<(SystemName, LifecycleStage), SystemState> Systems => systems;
  public Dictionary<(SystemName, LifecycleStage, ObjectName), ObjectState> Objects => objects;
}