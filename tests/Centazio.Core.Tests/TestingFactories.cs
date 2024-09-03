using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Func;
using Centazio.Core.Stage;

namespace Centazio.Core.Tests;

public static class TestingFactories {
  public static TestingStagedEntityStore SeStore() => new(); 
  public static TestingCtlRepository Repo() => new();
  public static IReadOperationRunner Runner(
      IStagedEntityStore? store = null, 
      ICtlRepository? repo = null) 
    => new DefaultReadOperationRunner(store ?? SeStore(), repo ?? Repo());
  
  public static Task<ReadOperationResult> TestingAbortingAndEmptyReadOperationImplementation(DateTime now, ReadOperationStateAndConfig op) {
    var result = Enum.Parse<EOperationReadResult>(op.Settings.Object); 
    return Task.FromResult(new EmptyReadOperationResult(
        Enum.Parse<EOperationReadResult>(op.Settings.Object), 
        String.Empty,
        result == EOperationReadResult.FailedRead ? EOperationAbortVote.Abort : EOperationAbortVote.Continue
    ) as ReadOperationResult);
  }
  
  public static Task<ReadOperationResult> TestingEmptyReadOperationImplementation(DateTime now, ReadOperationStateAndConfig op) => 
      Task.FromResult(new EmptyReadOperationResult(Enum.Parse<EOperationReadResult>(op.Settings.Object), String.Empty) as ReadOperationResult);
  
  public static Task<ReadOperationResult> TestingSingleReadOperationImplementation(DateTime now, ReadOperationStateAndConfig op) => 
      Task.FromResult(new SingleRecordReadOperationResult(Enum.Parse<EOperationReadResult>(op.Settings.Object), String.Empty, new (Guid.NewGuid().ToString())) as ReadOperationResult);
  
  public static Task<ReadOperationResult> TestingListReadOperationImplementation(DateTime now, ReadOperationStateAndConfig op) => 
      Task.FromResult(new ListRecordReadOperationResult(Enum.Parse<EOperationReadResult>(op.Settings.Object), String.Empty, 
          new ValidList<string>(Enumerable.Range(0, 100).Select(_ => Guid.NewGuid().ToString()).ToList())) as ReadOperationResult);
  
  public static string TestingChecksum(string data) => data.GetHashCode().ToString(); // simple fast 
}

public class TestingStagedEntityStore() : InMemoryStagedEntityStore(0, TestingFactories.TestingChecksum) { public List<StagedEntity> Contents => saved.ToList(); }

public class TestingCtlRepository : InMemoryCtlRepository {
  public Dictionary<(SystemName, LifecycleStage), SystemState> Systems => systems;
  public Dictionary<(SystemName, LifecycleStage, ObjectName), ObjectState> Objects => objects;
}