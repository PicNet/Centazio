using Centazio.Core;
using centazio.core.Ctl;
using centazio.core.Ctl.Entities;
using Centazio.Core.Func;
using Centazio.Core.Stage;
using Centazio.Test.Lib;

namespace centazio.core.tests.Read;

public static class ReadTestFactories {
  public static TestingStagedEntityStore SeStore() => new(); 
  public static TestingCtlRepository Repo(TestingUtcDate now) => new(now);
  public static IReadOperationRunner Runner(
      TestingUtcDate now, 
      IStagedEntityStore? store = null, 
      ICtlRepository? repo = null, 
      Func<DateTime, ReadOperationStateAndConfig, Task<ReadOperationResult>>? impl = null) 
    => new DefaultReadOperationRunner(
        impl ?? TestingAbortingAndEmptyReadOperationImplementation, 
        store ?? SeStore(), 
        now, 
        repo ?? Repo(now));

  public static ReadFunctionComposer Composer(TestingUtcDate now, ReadFunctionConfig cfg, IReadOperationRunner runner, ICtlRepository repo) => new(cfg, now, repo, runner);
  
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
      Task.FromResult(new SingleRecordReadOperationResult(Enum.Parse<EOperationReadResult>(op.Settings.Object), String.Empty, Guid.NewGuid().ToString()) as ReadOperationResult);
  
  public static Task<ReadOperationResult> TestingListReadOperationImplementation(DateTime now, ReadOperationStateAndConfig op) => 
      Task.FromResult(new ListRecordReadOperationResult(Enum.Parse<EOperationReadResult>(op.Settings.Object), String.Empty, Enumerable.Range(0, 100).Select(_ => Guid.NewGuid().ToString()).ToList()) as ReadOperationResult);

}

public class TestingStagedEntityStore() : InMemoryStagedEntityStore(0) { public List<StagedEntity> Contents => saved.ToList(); }

public class TestingCtlRepository(IUtcDate now) : InMemoryCtlRepository(now) {
  public Dictionary<(SystemName, LifecycleStage), SystemState> Systems => systems;
  public Dictionary<(SystemName, LifecycleStage, ObjectName), ObjectState> Objects => objects;
}