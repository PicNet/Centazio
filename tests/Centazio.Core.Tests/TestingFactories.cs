using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Func;
using Centazio.Core.Runner;
using Centazio.Core.Stage;

namespace Centazio.Core.Tests;

public static class TestingFactories {
    
  public static TestingStagedEntityStore SeStore() => new(); 
  public static TestingCtlRepository CtlRepo() => new();
  public static IOperationRunner<ReadOperationConfig> ReadRunner(IStagedEntityStore? store = null) 
      => new ReadOperationRunner(store ?? SeStore());
  
  public static Task<OperationResult> TestingAbortingAndEmptyReadOperationImplementation(OperationStateAndConfig<ReadOperationConfig> op) {
    var result = Enum.Parse<EOperationResult>(op.Settings.Object); 
    return Task.FromResult(new EmptyOperationResult(
        Enum.Parse<EOperationResult>(op.Settings.Object), 
        String.Empty,
        result == EOperationResult.Error ? EOperationAbortVote.Abort : EOperationAbortVote.Continue
    ) as OperationResult);
  }
  
  public static Task<OperationResult> TestingEmptyReadOperationImplementation(OperationStateAndConfig<ReadOperationConfig> op) => 
      Task.FromResult(new EmptyOperationResult(Enum.Parse<EOperationResult>(op.Settings.Object), String.Empty) as OperationResult);
  
  public static Task<OperationResult> TestingSingleReadOperationImplementation(OperationStateAndConfig<ReadOperationConfig> op) => 
      Task.FromResult(new SingleRecordOperationResult(Enum.Parse<EOperationResult>(op.Settings.Object), String.Empty, Guid.NewGuid().ToString()) as OperationResult);
  
  public static Task<OperationResult> TestingListReadOperationImplementation(OperationStateAndConfig<ReadOperationConfig> op) => 
      Task.FromResult(new ListRecordOperationResult(Enum.Parse<EOperationResult>(op.Settings.Object), String.Empty, 
          new ValidList<string>(Enumerable.Range(0, 100).Select(_ => Guid.NewGuid().ToString()).ToList())) as OperationResult);
  
  public static string TestingChecksum(string data) => data.GetHashCode().ToString(); // simple fast 
}

public class TestingStagedEntityStore() : InMemoryStagedEntityStore(0, TestingFactories.TestingChecksum) { public List<StagedEntity> Contents => saved.ToList(); }

public class TestingCtlRepository : InMemoryCtlRepository {
  public Dictionary<(SystemName, LifecycleStage), SystemState> Systems => systems;
  public Dictionary<(SystemName, LifecycleStage, ObjectName), ObjectState> Objects => objects;
}