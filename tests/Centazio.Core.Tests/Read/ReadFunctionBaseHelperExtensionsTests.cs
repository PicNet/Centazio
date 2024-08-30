using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Func;
using Centazio.Test.Lib;
using F = Centazio.Core.Tests.Read.ReadTestFactories;

namespace Centazio.Core.Tests.Read;

public class ReadFunctionBaseHelperExtensionsTests {

  private const string NAME = nameof(ReadFunctionBaseHelperExtensionsTests);
  private TestingCtlRepository repo;
  
  [SetUp] public void SetUp() => repo = ReadTestFactories.Repo();
  [TearDown] public async Task TearDown() => await repo.DisposeAsync();

  [Test] public void Test_ReadFunctionConfig_Validate_fails_with_empty_operations() {
    Assert.Throws<Exception>(() => new ReadFunctionConfig(NAME, NAME, []).Validate());
  }
  
  [Test] public async Task Test_LoadOperationsStates_creates_missing_operations() {
    var ss = await repo.CreateSystemState(NAME, NAME);
    var template = await repo.CreateObjectState(ss, "2"); 
    
    var cfg = new ReadFunctionConfig(NAME, NAME, Factories.OP_CONFIGS);
    var states = await cfg.Operations.LoadOperationsStates(ss, repo);
    
    Assert.That(states, Has.Count.EqualTo(4));
    Enumerable.Range(0, 4).ForEachIdx(TestAtIndex);
    
    async void TestAtIndex(int idx) {
      var name = (idx + 1).ToString();
      var exp = template with { Object = name };
      var actual = states[idx].State;
      
      Assert.That(actual, Is.EqualTo(exp));
      Assert.That(actual, Is.EqualTo(await repo.GetObjectState(ss, name)));
    }
  }
  
  [Test] public async Task Test_LoadOperationsStates_ignores_innactive_states() {
    var ss = await repo.CreateSystemState(NAME, NAME);
    var updated = await repo.CreateObjectState(ss, "2") with { Active = false };
    await repo.SaveObjectState(updated);
    
    var states = await Factories.OP_CONFIGS.LoadOperationsStates(ss, repo);
    var names = states.Select(s => s.Settings.Object.Value).ToList();
    Assert.That(names, Is.EquivalentTo(new [] {"1", "3", "4"}));
  }
  
  [Test] public void Test_GetReadyOperations_correctly_filters_out_operations_not_meeting_cron_criteria() {
    ReadOperationStateAndConfig Op(string name, string cron, DateTime last) => new(new(name, name, name, true, UtcDate.UtcNow, LastCompleted: last), new(name, new (cron), F.TestingListReadOperationImplementation));
    DateTime Dt(string dt) => DateTime.Parse(dt).ToUniversalTime();

    var now = Dt("2024-08-01T01:30:00Z");                 // 01:30 UTC on August 1st, 2024 
    var ops = new [] {
      Op("1", "0 0 * * *", Dt("2024-07-31T23:59:59Z")),   // Daily at 00:00 UTC, ready for 01:00 UTC
      Op("2", "0 * * * *", Dt("2024-08-01T00:00:00Z")),   // Hourly at 00 minutes, ready for 01:00 UTC
      Op("3", "0 0 1 * *", Dt("2024-07-01T00:00:00Z")),   // Monthly on the 1st at 00:00 UTC, ready for 2024-08-01
      
      Op("4", "0 0 * * *", Dt("2024-08-01T00:00:00Z")),   // Daily at 00:00 UTC, not yet ready at 01:00 UTC
      Op("5", "0 0 * * MON", Dt("2024-07-29T00:00:00Z")), // Weekly on Monday at 00:00 UTC, now is not Monday
      Op("6", "0 * * * *", Dt("2024-08-01T01:00:00Z"))    // Hourly at 00 minutes, not yet ready
    } ;
    var ready = ops.GetReadyOperations(now); 
    Assert.That(ready.Select(op => op.State.Object.Value), Is.EquivalentTo(new [] { "1", "2", "3" }));
  }
  
  [Test] public async Task Test_RunOperationsTillAbort_on_single_valid_op() {
    var runner = ReadTestFactories.Runner(repo: repo);
    
    var states1 = new List<ReadOperationStateAndConfig> { await Factories.CreateReadOpStateAndConf(EOperationReadResult.Success, repo) };
    var results1 = await states1.RunOperationsTillAbort(runner, repo, UtcDate.UtcNow);
    
    var states2 = new List<ReadOperationStateAndConfig> { await Factories.CreateReadOpStateAndConf(EOperationReadResult.Warning, repo) };
    var results2 = await states2.RunOperationsTillAbort(runner, repo, UtcDate.UtcNow);

    var states3 = new List<ReadOperationStateAndConfig> { await Factories.CreateReadOpStateAndConf(EOperationReadResult.FailedRead, repo) };
    var results3 = await states3.RunOperationsTillAbort(runner, repo, UtcDate.UtcNow);
    
    var newstates = repo.Objects.Values.ToList();
    
    Assert.That(results1, Is.EquivalentTo(new [] { new EmptyReadOperationResult(EOperationReadResult.Success, "" )}));
    Assert.That(results2, Is.EquivalentTo(new [] { new EmptyReadOperationResult(EOperationReadResult.Warning, "" )}));
    Assert.That(results3, Is.EquivalentTo(new [] { new EmptyReadOperationResult(EOperationReadResult.FailedRead, "", EOperationAbortVote.Abort )}));
    
    Assert.That(newstates, Has.Count.EqualTo(3));
    Assert.That(newstates[0], Is.EqualTo(ExpObjState(EOperationReadResult.Success, EOperationAbortVote.Continue)));
    Assert.That(newstates[1], Is.EqualTo(ExpObjState(EOperationReadResult.Warning, EOperationAbortVote.Continue)));
    Assert.That(newstates[2], Is.EqualTo(ExpObjState(EOperationReadResult.FailedRead, EOperationAbortVote.Abort)));
  }

  [Test] public async Task Test_RunOperationsTillAbort_stops_on_first_abort() {
    var runner = ReadTestFactories.Runner(repo: repo);
    
    var states = new List<ReadOperationStateAndConfig> {
      await Factories.CreateReadOpStateAndConf(EOperationReadResult.Warning, repo),
      await Factories.CreateReadOpStateAndConf(EOperationReadResult.FailedRead, repo),
      await Factories.CreateReadOpStateAndConf(EOperationReadResult.Success, repo)
    };
    
    var results = await states.RunOperationsTillAbort(runner, repo, UtcDate.UtcNow);
    var newstates = repo.Objects.Values.ToList();
    
    Assert.That(results, Is.EquivalentTo(new [] { 
      new EmptyReadOperationResult(EOperationReadResult.Warning, "" ),
      new EmptyReadOperationResult(EOperationReadResult.FailedRead, "", EOperationAbortVote.Abort )
    }));
    
    Assert.That(newstates, Has.Count.EqualTo(3));
    Assert.That(newstates[0], Is.EqualTo(ExpObjState(EOperationReadResult.Warning, EOperationAbortVote.Continue)));
    Assert.That(newstates[1], Is.EqualTo(ExpObjState(EOperationReadResult.FailedRead, EOperationAbortVote.Abort)));
    Assert.That(newstates[2], Is.EqualTo(states[2].State)); // remained unchanged
  }
  
  [Test] public async Task Test_RunOperationsTillAbort_stops_on_first_exception() {
    var runner = ReadTestFactories.Runner(repo: repo);
    
    var states = new List<ReadOperationStateAndConfig> {
      await Factories.CreateReadOpStateAndConf(EOperationReadResult.Warning, repo),
      await Factories.CreateReadOpStateAndConf(EOperationReadResult.FailedRead, repo),
      await Factories.CreateReadOpStateAndConf(EOperationReadResult.Success, repo)
    };
    states[1] = states[1] with { Settings = states[1].Settings with { Impl = (_, _) => throw new Exception() } }; 
    var results = (await states.RunOperationsTillAbort(runner, repo, UtcDate.UtcNow)).ToList();
    var (failex, failmsg) = (results[1].Exception, results[1].Message);
    var newstates = repo.Objects.Values.ToList(); 
    
    Assert.That(results, Has.Count.EqualTo(2));
    Assert.That(results[0], Is.EqualTo(new EmptyReadOperationResult(EOperationReadResult.Warning, "" )));
    Assert.That(failex, Is.Not.Null);
    Assert.That(String.IsNullOrWhiteSpace(failmsg), Is.False);
    Assert.That(results[1], Is.EqualTo(new EmptyReadOperationResult(EOperationReadResult.FailedRead, failmsg, EOperationAbortVote.Abort, failex )));
    
    Assert.That(newstates, Has.Count.EqualTo(3));
    Assert.That(newstates[0], Is.EqualTo(ExpObjState(EOperationReadResult.Warning, EOperationAbortVote.Continue)));
    var exp2 = ExpObjState(EOperationReadResult.FailedRead, EOperationAbortVote.Abort);
    Assert.That(newstates[1], Is.EqualTo(exp2 with { LastRunMessage = exp2.LastRunMessage + failmsg, LastRunException = failex.ToString() }));
    Assert.That(newstates[2], Is.EqualTo(states[2].State)); // remained unchanged
  }
  
  private ObjectState ExpObjState(EOperationReadResult res, EOperationAbortVote vote) => 
        new(res.ToString(), res.ToString(), res.ToString(), true, UtcDate.UtcNow, res, vote,
            UtcDate.UtcNow, UtcDate.UtcNow, UtcDate.UtcNow, $"read operation [{res}/{res}/{res}] completed [{res}] message: ", 0);
  
  static class Factories {
    public static async Task<ReadOperationStateAndConfig> CreateReadOpStateAndConf(EOperationReadResult result, ICtlRepository repo) 
        => new (
            await repo.CreateObjectState(await repo.CreateSystemState(result.ToString(), result.ToString()), result.ToString()), 
            new (result.ToString(), new ("* * * * *"), ReadTestFactories.TestingAbortingAndEmptyReadOperationImplementation));
    
    public static List<ReadOperationConfig> OP_CONFIGS => [
      new ReadOperationConfig("1", new ("* * * * *"), F.TestingEmptyReadOperationImplementation),
      new ReadOperationConfig("2", new ("* * * * *"), F.TestingEmptyReadOperationImplementation),
      new ReadOperationConfig("3", new ("* * * * *"), F.TestingEmptyReadOperationImplementation),
      new ReadOperationConfig("4", new ("* * * * *"), F.TestingEmptyReadOperationImplementation)
    ];
  }
}