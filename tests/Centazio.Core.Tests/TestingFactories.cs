using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Promote;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Core.Stage;

namespace Centazio.Core.Tests;

public static class TestingFactories {
    
  public static TestingStagedEntityStore SeStore() => new(); 
  public static TestingCtlRepository CtlRepo() => new();
  public static IOperationRunner<ReadOperationConfig, ReadOperationResult> ReadRunner(IStagedEntityStore? store = null) => new ReadOperationRunner(store ?? SeStore());
  public static IOperationRunner<PromoteOperationConfig, PromoteOperationResult> PromoteRunner(IStagedEntityStore? store = null) => new PromoteOperationRunner(store ?? SeStore());
  
  public static Task<ReadOperationResult> TestingAbortingAndEmptyReadOperationImplementation(OperationStateAndConfig<ReadOperationConfig> op) {
    var result = Enum.Parse<EOperationResult>(op.Settings.Object); 
    return Task.FromResult(result == EOperationResult.Error ? new ReadOperationResult(OperationResult.Error(EOperationAbortVote.Abort)) : new ReadOperationResult(OperationResult.Empty()));
  }
  
  public static Task<ReadOperationResult> TestingEmptyReadOperationImplementation(OperationStateAndConfig<ReadOperationConfig> op) {
    var result = Enum.Parse<EOperationResult>(op.Settings.Object); 
    return Task.FromResult(result == EOperationResult.Error ? new ReadOperationResult(OperationResult.Error()) : new ReadOperationResult(OperationResult.Empty()));
  }

  public static Task<ReadOperationResult> TestingSingleReadOperationImplementation(OperationStateAndConfig<ReadOperationConfig> op) {
    var result = Enum.Parse<EOperationResult>(op.Settings.Object); 
    return Task.FromResult(result == EOperationResult.Error ? new ReadOperationResult(OperationResult.Error()) : new ReadOperationResult(OperationResult.Success(Guid.NewGuid().ToString())));
  }

  public static Task<ReadOperationResult> TestingListReadOperationImplementation(OperationStateAndConfig<ReadOperationConfig> op) {
    var result = Enum.Parse<EOperationResult>(op.Settings.Object); 
    return Task.FromResult(result == EOperationResult.Error ? new ReadOperationResult(OperationResult.Error()) : new ReadOperationResult(OperationResult.Success(Enumerable.Range(0, 100).Select(_ => Guid.NewGuid().ToString())))); 
  }

  public static string TestingChecksum(string data) => data.GetHashCode().ToString(); // simple fast 
}

public class TestingStagedEntityStore() : InMemoryStagedEntityStore(0, TestingFactories.TestingChecksum) { public List<StagedEntity> Contents => saved.ToList(); }

public class TestingCtlRepository : InMemoryCtlRepository {
  public Dictionary<(SystemName, LifecycleStage), SystemState> Systems => systems;
  public Dictionary<(SystemName, LifecycleStage, ObjectName), ObjectState> Objects => objects;
}