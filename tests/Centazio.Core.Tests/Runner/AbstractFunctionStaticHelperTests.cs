using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Test.Lib;

namespace Centazio.Core.Tests.Runner;

public class AbstractFunctionStaticHelperTests {
  
  private TestingInMemoryBaseCtlRepository repo;
  
  [SetUp] public void SetUp() => repo = F.CtlRepo();
  [TearDown] public async Task TearDown() => await repo.DisposeAsync();

  [Test] public void Test_ReadFunctionConfig_Validate_fails_with_empty_operations() {
    Assert.Throws<ArgumentNullException>(() => _ = new FunctionConfig<ReadOperationConfig>([]));
  }
  
  [Test] public async Task Test_LoadOperationsStates_creates_missing_operations() {
    var ss = await repo.CreateSystemState(C.System1Name, LifecycleStage.Defaults.Read);
    var cfg = new FunctionConfig<ReadOperationConfig>(Factories.READ_OP_CONFIGS);
    var template = await repo.CreateObjectState(ss, new SystemEntityTypeName("2"), cfg.DefaultFirstTimeCheckpoint); 
    
    var states = await AbstractFunction<ReadOperationConfig>.LoadOperationsStates(cfg, ss, repo);
    
    Assert.That(states, Has.Count.EqualTo(4));
    await Enumerable.Range(0, 4).Select(TestAtIndex).Synchronous();
    
    async Task TestAtIndex(int idx) {
      var name = (idx + 1).ToString();
      var exp = template with { Object = new SystemEntityTypeName(name) };
      var actual = states[idx].State;
      
      Assert.That(actual, Is.EqualTo(exp));
      Assert.That(actual, Is.EqualTo(await repo.GetObjectState(ss, new SystemEntityTypeName(name))));
    }
  }
  
  [Test] public async Task Test_LoadOperationsStates_ignores_innactive_states() {
    var ss = await repo.CreateSystemState(C.System1Name, LifecycleStage.Defaults.Read);
    var updated = (await repo.CreateObjectState(ss, new SystemEntityTypeName("2"), UtcDate.UtcNow)).SetActive(false);
    await repo.SaveObjectState(updated);
    
    var config = new FunctionConfig<ReadOperationConfig>(Factories.READ_OP_CONFIGS) { ChecksumAlgorithm = new Helpers.ChecksumAlgo() };
    var states = await AbstractFunction<ReadOperationConfig>.LoadOperationsStates(config, ss, repo);
    var names = states.Select(s => s.OpConfig.Object.Value).ToList();
    Assert.That(names, Is.EquivalentTo(["1", "3", "4"]));
  }
  
  [Test] public void Test_GetReadyOperations_correctly_filters_out_operations_not_meeting_cron_criteria() {
    OperationStateAndConfig<ReadOperationConfig> Op(string name, string cron, DateTime last) => new(
        new ObjectState(new(name), new(name), new SystemEntityTypeName(name), UtcDate.UtcNow, true) { 
          LastCompleted = last,
          LastResult = EOperationResult.Success,
          LastAbortVote = EOperationAbortVote.Continue
        }, 
        new BaseFunctionConfig(), 
        new(new SystemEntityTypeName(name), new (new (cron)), GetListResult), 
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
    var runner = F.ReadRunner();
    
    var states1 = new List<OperationStateAndConfig<ReadOperationConfig>> { await Factories.CreateReadOpStateAndConf(EOperationResult.Success, repo) };
    var results1 = await AbstractFunction<ReadOperationConfig>.RunOperationsTillAbort(states1, runner, repo);
    
    var states2 = new List<OperationStateAndConfig<ReadOperationConfig>> { await Factories.CreateReadOpStateAndConf(EOperationResult.Error, repo) };
    var results2 = await AbstractFunction<ReadOperationConfig>.RunOperationsTillAbort(states2, runner, repo);
    
    var newstates = repo.Objects.Values.ToList();

    Assert.That(results1, Is.EquivalentTo([new EmptyReadOperationResult()]));
    Assert.That(results2, Is.EquivalentTo([new ErrorReadOperationResult(EOperationAbortVote.Abort)]));
    
    Assert.That(newstates, Has.Count.EqualTo(2));
    Assert.That(newstates[0], Is.EqualTo(ExpObjState(EOperationResult.Success, EOperationAbortVote.Continue, 0, UtcDate.UtcNow)));
    Assert.That(newstates[1], Is.EqualTo(ExpObjState(EOperationResult.Error, EOperationAbortVote.Abort, 0, UtcDate.UtcNow)));
  }

  [Test] public async Task Test_RunOperationsTillAbort_stops_on_first_abort() {
    var runner = F.ReadRunner();
    
    var states = new List<OperationStateAndConfig<ReadOperationConfig>> {
      await Factories.CreateReadOpStateAndConf(EOperationResult.Error, repo),
      await Factories.CreateReadOpStateAndConf(EOperationResult.Success, repo)
    };
    
    var results = await AbstractFunction<ReadOperationConfig>.RunOperationsTillAbort(states, runner, repo);
    var newstates = repo.Objects.Values.ToList();
    
    Assert.That(results, Is.EquivalentTo([
      new ErrorReadOperationResult(EOperationAbortVote.Abort)
    ]));
    
    Assert.That(newstates, Has.Count.EqualTo(2));
    Assert.That(newstates[0], Is.EqualTo(ExpObjState(EOperationResult.Error, EOperationAbortVote.Abort, 0, UtcDate.UtcNow)));
    Assert.That(newstates[1], Is.EqualTo(states[1].State)); // remained unchanged
  }
  
  [Test] public async Task Test_RunOperationsTillAbort_stops_on_first_exception() {
    var runner = F.ReadRunner();
    
    var states = new List<OperationStateAndConfig<ReadOperationConfig>> {
      await Factories.CreateReadOpStateAndConf(EOperationResult.Error, repo),
      await Factories.CreateReadOpStateAndConf(EOperationResult.Success, repo)
    };
    states[0] = states[0] with { OpConfig = states[0].OpConfig with { GetUpdatesAfterCheckpoint = ThrowsError } }; 
    var results = (await AbstractFunction<ReadOperationConfig>.RunOperationsTillAbort(states, runner, repo, false)).ToList();
    var newstates = repo.Objects.Values.ToList();
    var ex = results[0].Exception ?? throw new Exception();

    Assert.That(results, Has.Count.EqualTo(1));
    Assert.That(results[0], Is.EqualTo(new ErrorReadOperationResult(EOperationAbortVote.Abort, ex)));
    
    Assert.That(newstates, Has.Count.EqualTo(2));
    var exp2 = ExpObjState(EOperationResult.Error, EOperationAbortVote.Abort, 0, UtcDate.UtcNow, ex.Message);
    Assert.That(newstates[0], Is.EqualTo(exp2 with { LastRunException = ex.ToString() }));
    Assert.That(newstates[1], Is.EqualTo(states[1].State)); // remained unchanged
  }
  
  [Test] public async Task Test_RunOperations_does_not_update_NextCheckpoint_on_error() {
    var (name, startingcp, runner) = (nameof(AbstractFunctionStaticHelperTests), UtcDate.UtcNow.AddMinutes(1), F.ReadRunner());
    var sysstate = await repo.CreateSystemState(new(name), LifecycleStage.Defaults.Read);
    var objstate = await repo.CreateObjectState(sysstate, new(name), startingcp);
    var readopcfg = new ReadOperationConfig(new(EOperationResult.Error.ToString()), TestingDefaults.CRON_EVERY_SECOND, GetAbortingOrEmptyResult);
    var opconfigs = new List<OperationStateAndConfig<ReadOperationConfig>> {
      new(objstate, new BaseFunctionConfig(), readopcfg, objstate.NextCheckpoint)
    };
    
    TestingUtcDate.DoTick();
    await AbstractFunction<ReadOperationConfig>.RunOperationsTillAbort(opconfigs, runner, repo, false);
    TestingUtcDate.DoTick();
    
    var loaded = await repo.GetObjectState(sysstate, new(name)) ?? throw new Exception();
    Assert.That(loaded.NextCheckpoint, Is.EqualTo(startingcp));
  }
  
  [Test] public async Task Test_RunOperations_sets_NextCheckpoint_to_op_start_when_empty() {
    var (name, startingcp, runner) = (nameof(AbstractFunctionStaticHelperTests), UtcDate.UtcNow.AddMinutes(1), F.ReadRunner());
    var sysstate = await repo.CreateSystemState(new(name), LifecycleStage.Defaults.Read);
    var objstate = await repo.CreateObjectState(sysstate, new(name), startingcp);
    var readopcfg = new ReadOperationConfig(new(EOperationResult.Success.ToString()), TestingDefaults.CRON_EVERY_SECOND, GetAbortingOrEmptyResult);
    var opconfigs = new List<OperationStateAndConfig<ReadOperationConfig>> {
      new(objstate, new BaseFunctionConfig(), readopcfg, objstate.NextCheckpoint)
    };
    
    var opstart = TestingUtcDate.DoTick();
    await AbstractFunction<ReadOperationConfig>.RunOperationsTillAbort(opconfigs, runner, repo, false);
    TestingUtcDate.DoTick();
    
    var loaded = await repo.GetObjectState(sysstate, new(name)) ?? throw new Exception();
    Assert.That(loaded.NextCheckpoint, Is.EqualTo(opstart));
  }
  
  [Test] public async Task Test_RunOperations_sets_NextCheckpoint_to_result_when_non_empty_success() {
    var (name, startingcp, successcp, runner) = (nameof(AbstractFunctionStaticHelperTests), UtcDate.UtcNow.AddMinutes(1), UtcDate.UtcNow.AddMinutes(2), F.ReadRunner());
    var sysstate = await repo.CreateSystemState(new(name), LifecycleStage.Defaults.Read);
    var objstate = await repo.CreateObjectState(sysstate, new(name), startingcp);
    var readopcfg = new ReadOperationConfig(new(EOperationResult.Success.ToString()), TestingDefaults.CRON_EVERY_SECOND, config => GetListResult(config, successcp));
    var opconfigs = new List<OperationStateAndConfig<ReadOperationConfig>> {
      new(objstate, new BaseFunctionConfig(), readopcfg, objstate.NextCheckpoint)
    };
    TestingUtcDate.DoTick();
    await AbstractFunction<ReadOperationConfig>.RunOperationsTillAbort(opconfigs, runner, repo, false);
    TestingUtcDate.DoTick();
    
    var loaded = await repo.GetObjectState(sysstate, new(name)) ?? throw new Exception();
    Assert.That(loaded.NextCheckpoint, Is.EqualTo(successcp));
  }
  
  private ObjectState ExpObjState(EOperationResult res, EOperationAbortVote vote, int len, DateTime nextcheckpoint, string exmessage="na") {
    return new ObjectState(new(res.ToString()), new(res.ToString()), new SystemEntityTypeName(res.ToString()), nextcheckpoint, true) {
      DateCreated = UtcDate.UtcNow,
      LastResult = res, 
      LastAbortVote = vote, 
      DateUpdated = UtcDate.UtcNow, 
      LastStart = UtcDate.UtcNow, 
      LastSuccessStart = res == EOperationResult.Success ? UtcDate.UtcNow : null, 
      LastCompleted = UtcDate.UtcNow,
      LastSuccessCompleted = res == EOperationResult.Success ? UtcDate.UtcNow : null,
      LastRunMessage = $"operation [{res}/{res}/{res}] completed [{res}] message: " +
          (len == 0 
              ? res == EOperationResult.Error ? $"ErrorReadOperationResult[{exmessage}] - AbortVote[Abort]" : "EmptyReadOperationResult" 
              : String.Empty)
    };
  }

  static class Factories {
    public static async Task<OperationStateAndConfig<ReadOperationConfig>> CreateReadOpStateAndConf(EOperationResult result, ICtlRepository repo) 
        => new (
            await repo.CreateObjectState(await repo.CreateSystemState(new(result.ToString()), new(result.ToString())), new SystemEntityTypeName(result.ToString()), UtcDate.UtcNow),
            new BaseFunctionConfig(),
            new (new SystemEntityTypeName(result.ToString()), new (new (TestingDefaults.CRON_EVERY_SECOND)), GetAbortingOrEmptyResult), 
            DateTime.MinValue);
    
    public static List<ReadOperationConfig> READ_OP_CONFIGS => [
      new(new SystemEntityTypeName("1"), new(new(TestingDefaults.CRON_EVERY_SECOND)), GetEmptyResults),
      new(new SystemEntityTypeName("2"), new(new(TestingDefaults.CRON_EVERY_SECOND)), GetEmptyResults),
      new(new SystemEntityTypeName("3"), new(new(TestingDefaults.CRON_EVERY_SECOND)), GetEmptyResults),
      new(new SystemEntityTypeName("4"), new(new(TestingDefaults.CRON_EVERY_SECOND)), GetEmptyResults)
    ];
  }
  
  private static Task<ReadOperationResult> GetListResult(OperationStateAndConfig<ReadOperationConfig> config) => GetListResult(config, null);
  
  private static Task<ReadOperationResult> GetListResult(OperationStateAndConfig<ReadOperationConfig> config, DateTime? nextcheckpoint) {
    ReadOperationResult res = Enum.Parse<EOperationResult>(config.OpConfig.Object) == EOperationResult.Error 
        ? new ErrorReadOperationResult() 
        : new ListReadOperationResult(Enumerable.Range(0, 100).Select(_ => Guid.NewGuid().ToString()).ToList(), nextcheckpoint ?? UtcDate.UtcNow);
    return Task.FromResult(res); 
  }
  
  private static Task<ReadOperationResult> GetAbortingOrEmptyResult(OperationStateAndConfig<ReadOperationConfig> config) {
    ReadOperationResult res = Enum.Parse<EOperationResult>(config.OpConfig.Object) == EOperationResult.Error 
        ? new ErrorReadOperationResult(EOperationAbortVote.Abort) 
        : new EmptyReadOperationResult(); 
    return Task.FromResult(res);
  }
  
  private static Task<ReadOperationResult> GetEmptyResults(OperationStateAndConfig<ReadOperationConfig> config) {
    ReadOperationResult res = Enum.Parse<EOperationResult>(config.OpConfig.Object) == EOperationResult.Error 
        ? new ErrorReadOperationResult() 
        : new EmptyReadOperationResult();
    return Task.FromResult(res);
  }
  
  
  private Task<ReadOperationResult> ThrowsError(OperationStateAndConfig<ReadOperationConfig> _) => throw new Exception();
}