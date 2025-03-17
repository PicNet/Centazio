using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Core.Settings;
using Centazio.Test.Lib;

namespace Centazio.Core.Tests.Runner;

public class AbstractFunctionTests {

  private readonly string name = nameof(AbstractFunctionTests);
  private TestingInMemoryBaseCtlRepository repo;
  
  [SetUp] public void SetUp() => repo = F.CtlRepo();
  [TearDown] public async Task TearDown() => await repo.DisposeAsync();

  [Test] public void Test_ReadFunctionConfig_Validate_fails_with_empty_operations() {
    Assert.Throws<ArgumentNullException>(() => _ = new FunctionConfig([]));
  }
  
  [Test] public async Task Test_LoadOperationsStates_creates_missing_operations() {
    var ss = await repo.CreateSystemState(C.System1Name, LifecycleStage.Defaults.Read);
    var cfg = new FunctionConfig(Factories.READ_OP_CONFIGS);
    var template = await repo.CreateObjectState(ss, new SystemEntityTypeName("2"), cfg.DefaultFirstTimeCheckpoint); 
    
    var states = await AbstractFunction<ReadOperationConfig>.LoadOperationsStates(cfg, ss, repo);
    
    Assert.That(states, Has.Count.EqualTo(4));
    await Enumerable.Range(0, 4).Select(TestAtIndex).Synchronous();
    
    async Task TestAtIndex(int idx) {
      var idxname = (idx + 1).ToString();
      var exp = template with { Object = new SystemEntityTypeName(idxname) };
      var actual = states[idx].State;
      
      Assert.That(actual, Is.EqualTo(exp));
      Assert.That(actual, Is.EqualTo(await repo.GetObjectState(ss, new SystemEntityTypeName(idxname))));
    }
  }
  
  [Test] public async Task Test_LoadOperationsStates_ignores_innactive_states() {
    var ss = await repo.CreateSystemState(C.System1Name, LifecycleStage.Defaults.Read);
    var updated = (await repo.CreateObjectState(ss, new SystemEntityTypeName("2"), UtcDate.UtcNow)).SetActive(false);
    await repo.SaveObjectState(updated);
    
    var config = new FunctionConfig(Factories.READ_OP_CONFIGS) { ChecksumAlgorithm = new Helpers.TestingHashcodeBasedChecksumAlgo() };
    var states = await AbstractFunction<ReadOperationConfig>.LoadOperationsStates(config, ss, repo);
    var names = states.Select(s => s.OpConfig.Object.Value).ToList();
    Assert.That(names, Is.EquivalentTo(["1", "3", "4"]));
  }
  
  [Test] public void Test_GetReadyOperations_correctly_filters_out_operations_not_meeting_cron_criteria() {
    OperationStateAndConfig<ReadOperationConfig> Op(string opname, string cron, DateTime last) => new(
        new ObjectState(new(opname), new(opname), new SystemEntityTypeName(opname), UtcDate.UtcNow, true) { 
          LastCompleted = last,
          LastResult = EOperationResult.Success,
          LastAbortVote = EOperationAbortVote.Continue
        }, 
        F.EmptyFunctionConfig(), 
        new(new SystemEntityTypeName(opname), new (new (cron)), GetListResult), 
        DateTime.MinValue);
    DateTime Dt(string dt) => DateTime.Parse(dt).ToUniversalTime();

    using var _ = new ShortLivedUtcDateOverride(
        Dt("2024-08-01T01:30:00Z"));                        // 01:30 UTC on August 1st, 2024
    
    var ops = new List<OperationStateAndConfig<ReadOperationConfig>> {
      Op("1", "0 0 0 * * *", Dt("2024-07-31T23:59:59Z")),   // Daily at 00:00 UTC, ready for 01:00 UTC
      Op("2", "0 0 * * * *", Dt("2024-08-01T00:00:00Z")),   // Hourly at 00 minutes, ready for 01:00 UTC
      Op("3", "0 0 0 1 * *", Dt("2024-07-01T00:00:00Z")),   // Monthly on the 1st at 00:00 UTC, ready for 2024-08-01
      
      Op("4", "0 0 0 * * *", Dt("2024-08-01T00:00:00Z")),   // Daily at 00:00 UTC, not yet ready at 01:00 UTC
      Op("5", "0 0 0 * * MON", Dt("2024-07-29T00:00:00Z")), // Weekly on Monday at 00:00 UTC, now is not Monday
      Op("6", "0 0 * * * *", Dt("2024-08-01T01:00:00Z"))    // Hourly at 00 minutes, not yet ready
    } ;
    var ready = AbstractFunction<ReadOperationConfig>.GetReadyOperations(ops); 
    Assert.That(ready.Select(op => op.State.Object.Value), Is.EquivalentTo(["1", "2", "3"]));
  }
  
  [Test] public async Task Test_RunOperationsTillAbort_on_single_valid_op() {
    var runner = F.ReadFunc(ctl: repo);
    
    var states1 = new List<OperationStateAndConfig<ReadOperationConfig>> { await F.CreateReadOpStateAndConf(repo) };
    var res1 = (await RunOps(states1, runner)).Single().Result;
    
    var states2 = new List<OperationStateAndConfig<ReadOperationConfig>> { await F.CreateErroringOpStateAndConf(repo) };
    var res2 = (await RunOps(states2, runner)).Single().Result;
    
    var newstates = repo.Objects.Values.ToList();

    Assert.That(res1, Is.EqualTo(new EmptyReadOperationResult()));
    Assert.That(res2, Is.EqualTo(new ErrorOperationResult(0, EOperationAbortVote.Abort, res2.Exception)));
    
    Assert.That(newstates, Has.Count.EqualTo(2));
    Assert.That(newstates[0], Is.EqualTo(ExpObjState(EOperationResult.Success, EOperationAbortVote.Continue, 0, UtcDate.UtcNow)));
    Assert.That(newstates[1], Is.EqualTo(ExpObjState(EOperationResult.Error, EOperationAbortVote.Abort, 0, UtcDate.UtcNow, res2.Exception)));
  }

  [Test] public async Task Test_RunOperationsTillAbort_stops_on_first_abort() {
    var runner = F.ReadFunc(ctl: repo);
    
    var states = new List<OperationStateAndConfig<ReadOperationConfig>> {
      await F.CreateErroringOpStateAndConf(repo),
      await F.CreateReadOpStateAndConf(repo)
    };
    
    var res = (await RunOps(states, runner)).Single().Result;
    var resex = res.Exception;
    var newstates = repo.Objects.Values.ToList();
    
    Assert.That(res, Is.EqualTo(new ErrorOperationResult(0, EOperationAbortVote.Abort, resex)));
    
    Assert.That(newstates, Has.Count.EqualTo(2));
    Assert.That(newstates[0], Is.EqualTo(ExpObjState(EOperationResult.Error, EOperationAbortVote.Abort, 0, UtcDate.UtcNow, resex)));
    Assert.That(newstates[1], Is.EqualTo(states[1].State)); // remained unchanged
  }
  
  [Test] public async Task Test_RunOperationsTillAbort_stops_on_first_exception() {
    var runner = F.ReadFunc(ctl: repo);
    
    var states = new List<OperationStateAndConfig<ReadOperationConfig>> {
      await F.CreateErroringOpStateAndConf(repo),
      await F.CreateReadOpStateAndConf(repo)
    };
    states[0] = states[0] with { OpConfig = states[0].OpConfig with { GetUpdatesAfterCheckpoint = ThrowsError } }; 
    var results = (await RunOps(states, runner)).ToList();
    var newstates = repo.Objects.Values.ToList();
    var ex = results[0].Result.Exception ?? throw new Exception();

    Assert.That(results, Has.Count.EqualTo(1));
    Assert.That(results[0].Result, Is.EqualTo(new ErrorOperationResult(0, EOperationAbortVote.Abort, ex)));
    
    Assert.That(newstates, Has.Count.EqualTo(2));
    var exp2 = ExpObjState(EOperationResult.Error, EOperationAbortVote.Abort, 0, UtcDate.UtcNow, ex);
    Assert.That(newstates[0], Is.EqualTo(exp2));
    Assert.That(newstates[1], Is.EqualTo(states[1].State)); // remained unchanged
  }
  
  [Test] public async Task Test_RunOperations_does_not_update_NextCheckpoint_on_error() {
    var (startingcp, runner) = (UtcDate.UtcNow.AddMinutes(1), F.ReadFunc(ctl: repo));
    var sysstate = await repo.CreateSystemState(new(name), LifecycleStage.Defaults.Read);
    var objstate = await repo.CreateObjectState(sysstate, new(name), startingcp);
    var readopcfg = new ReadOperationConfig(new(EOperationResult.Error.ToString()), TestingDefaults.CRON_EVERY_SECOND, _ => throw new Exception());
    var opconfigs = new List<OperationStateAndConfig<ReadOperationConfig>> {
      new(objstate, F.EmptyFunctionConfig(), readopcfg, objstate.NextCheckpoint)
    };
    
    TestingUtcDate.DoTick();
    await RunOps(opconfigs, runner);
    TestingUtcDate.DoTick();
    
    var loaded = await repo.GetObjectState(sysstate, new(name)) ?? throw new Exception();
    Assert.That(loaded.NextCheckpoint, Is.EqualTo(startingcp));
  }
  
  [Test] public async Task Test_RunOperations_sets_NextCheckpoint_to_op_start_when_empty() {
    var (startingcp, runner) = (UtcDate.UtcNow.AddMinutes(1), F.ReadFunc(ctl: repo));
    var sysstate = await repo.CreateSystemState(new(name), LifecycleStage.Defaults.Read);
    var objstate = await repo.CreateObjectState(sysstate, new(name), startingcp);
    var readopcfg = new ReadOperationConfig(new(EOperationResult.Success.ToString()), TestingDefaults.CRON_EVERY_SECOND, F.GetEmptyResult);
    var opconfigs = new List<OperationStateAndConfig<ReadOperationConfig>> {
      new(objstate, F.EmptyFunctionConfig(), readopcfg, objstate.NextCheckpoint)
    };
    
    var opstart = TestingUtcDate.DoTick();
    await RunOps(opconfigs, runner);
    TestingUtcDate.DoTick();
    
    var loaded = await repo.GetObjectState(sysstate, new(name)) ?? throw new Exception();
    Assert.That(loaded.NextCheckpoint, Is.EqualTo(opstart));
  }
  
  [Test] public async Task Test_RunOperations_sets_NextCheckpoint_to_result_when_non_empty_success() {
    var (startingcp, successcp, runner) = (UtcDate.UtcNow.AddMinutes(1), UtcDate.UtcNow.AddMinutes(2), new EmptyReadFunction(new(name), TestingFactories.SeRepo(), repo));
    var sysstate = await repo.CreateSystemState(new(name), LifecycleStage.Defaults.Read);
    var objstate = await repo.CreateObjectState(sysstate, new(name), startingcp);
    var readopcfg = new ReadOperationConfig(new(EOperationResult.Success.ToString()), TestingDefaults.CRON_EVERY_SECOND, config => GetListResult(config, successcp));
    var opconfigs = new List<OperationStateAndConfig<ReadOperationConfig>> {
      new(objstate, F.EmptyFunctionConfig(), readopcfg, objstate.NextCheckpoint)
    };
    TestingUtcDate.DoTick();
    await RunOps(opconfigs, runner);
    TestingUtcDate.DoTick();
    
    var loaded = await repo.GetObjectState(sysstate, new(name)) ?? throw new Exception();
    Assert.That(loaded.NextCheckpoint, Is.EqualTo(successcp));
  }
  
  [Test] public void Test_poll_seconds() {
    var defs = new DefaultsSettings {
      ConsoleCommands = null!,
      GeneratedCodeFolder = null!,
      FunctionMaxAllowedRunningMinutes = 0,
      ReadFunctionPollExpression = "*/1 * * * * *",
      PromoteFunctionPollExpression = "*/2 * * * * *",
      WriteFunctionPollExpression = "*/3 * * * * *",
      OtherFunctionPollExpression = "*/4 * * * * *"
    };
    Assert.That(((IRunnableFunction) F.ReadFunc()).GetFunctionPollCronExpression(defs), Is.EqualTo(new ValidCron("*/1 * * * * *")));
    Assert.That(((IRunnableFunction) F.PromoteFunc()).GetFunctionPollCronExpression(defs), Is.EqualTo(new ValidCron("*/2 * * * * *")));
    Assert.That(((IRunnableFunction) F.WriteFunc()).GetFunctionPollCronExpression(defs), Is.EqualTo(new ValidCron("*/3 * * * * *")));
  }
  
  private async Task<List<OpResultAndObject>> RunOps(List<OperationStateAndConfig<ReadOperationConfig>> ops, AbstractFunction<ReadOperationConfig> func) => 
      await func.RunOperationsTillAbort(ops, false);

  private ObjectState ExpObjState(EOperationResult res, EOperationAbortVote vote, int len, DateTime nextcheckpoint, Exception? ex = null) {
    return new ObjectState(new(res.ToString()), new(res.ToString()), new SystemEntityTypeName(res.ToString()), nextcheckpoint, true) {
      DateCreated = UtcDate.UtcNow,
      LastResult = res, 
      LastAbortVote = vote, 
      DateUpdated = UtcDate.UtcNow, 
      LastStart = UtcDate.UtcNow, 
      LastSuccessStart = res == EOperationResult.Success ? UtcDate.UtcNow : null, 
      LastCompleted = UtcDate.UtcNow,
      LastSuccessCompleted = res == EOperationResult.Success ? UtcDate.UtcNow : null,
      LastRunException = ex?.ToString(),
      LastRunMessage = $"operation [{res}/{res}/{res}] completed [{res}] message: " +
          (len == 0 
              ? res == EOperationResult.Error ?
                  ex is null ? throw new Exception()
                  : $"ErrorOperationResult[{ex.Message}] - AbortVote[Abort] ChangedCount[0]" : "EmptyReadOperationResult" 
              : String.Empty)
    };
  }

  static class Factories {
    
    
    public static List<OperationConfig> READ_OP_CONFIGS => [
      new ReadOperationConfig(new SystemEntityTypeName("1"), TestingDefaults.CRON_EVERY_SECOND, F.GetEmptyResult),
      new ReadOperationConfig(new SystemEntityTypeName("2"), TestingDefaults.CRON_EVERY_SECOND, F.GetEmptyResult),
      new ReadOperationConfig(new SystemEntityTypeName("3"), TestingDefaults.CRON_EVERY_SECOND, F.GetEmptyResult),
      new ReadOperationConfig(new SystemEntityTypeName("4"), TestingDefaults.CRON_EVERY_SECOND, F.GetEmptyResult)
    ];
  }
  
  private static Task<ReadOperationResult> GetListResult(OperationStateAndConfig<ReadOperationConfig> config) => GetListResult(config, null);
  
  private static Task<ReadOperationResult> GetListResult(OperationStateAndConfig<ReadOperationConfig> _, DateTime? nextcheckpoint) => 
      Task.FromResult<ReadOperationResult>(new ListReadOperationResult(Enumerable.Range(0, 100).Select(_ => Guid.NewGuid().ToString()).ToList(), nextcheckpoint ?? UtcDate.UtcNow));
  
  
  private Task<ReadOperationResult> ThrowsError(OperationStateAndConfig<ReadOperationConfig> _) => throw new Exception();
}