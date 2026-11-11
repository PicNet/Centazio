using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Core.Settings;
using Centazio.Test.Lib;

namespace Centazio.Core.Tests.Runner;

public class AbstractFunctionTests {

  private readonly string name = nameof(AbstractFunctionTests);
  private FakeTimeProvider clock;
  private TestingInMemoryBaseCtlRepository repo;
  
  [SetUp] public void SetUp() {
    clock = F.Clock();
    repo = F.CtlRepo(clock);
  }
  [TearDown] public async Task TearDown() => await repo.DisposeAsync();

  [Test] public void Test_ReadFunctionConfig_Validate_fails_with_empty_operations() {
    Assert.Throws<ArgumentNullException>(() => _ = new FunctionConfig([]));
  }
  
  [Test] public async Task Test_LoadOperationsStates_creates_missing_operations() {
    var ss = await repo.CreateSystemState(C.System1Name, LifecycleStage.Defaults.Read);
    var cfg = new FunctionConfig(Factories.READ_OP_CONFIGS);
    var template = await repo.CreateObjectState(ss, new SystemEntityTypeName("2"), clock.UtcNow().AddMonths(-1)); 
    
    var states = await AbstractFunction<ReadOperationConfig>.LoadOperationsStates(cfg, ss, repo, clock.UtcNow());
    
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
    var updated = (await repo.CreateObjectState(ss, new SystemEntityTypeName("2"), clock.UtcNow())).SetActive(false, clock.UtcNow());
    await repo.SaveObjectState(updated);
    
    var config = new FunctionConfig(Factories.READ_OP_CONFIGS) { ChecksumAlgorithm = new Helpers.TestingHashcodeBasedChecksumAlgo() };
    var states = await AbstractFunction<ReadOperationConfig>.LoadOperationsStates(config, ss, repo, clock.UtcNow());
    var names = states.Select(s => s.OpConfig.Object.Value).ToList();
    Assert.That(names, Is.EquivalentTo(["1", "3", "4"]));
  }
  
  [Test] public async Task Test_first_time_checkpoint_defaults_to_one_month_before_the_function_clock() {
    var first = new CheckpointRecordingFunction(new("first"), repo, clock);
    await F.RunFunc(clock, first, repo);
    var firstnow = clock.UtcNow();
    
    // a function created after time has moved must not see a default computed earlier (it was a static initialised once)
    clock.Tick(TimeSpan.FromDays(100));
    var second = new CheckpointRecordingFunction(new("second"), repo, clock);
    await F.RunFunc(clock, second, repo);
    
    Assert.That(first.Checkpoints.Single(), Is.EqualTo(firstnow.AddMonths(-1)));
    Assert.That(second.Checkpoints.Single(), Is.EqualTo(clock.UtcNow().AddMonths(-1)));
  }
  
  [Test] public async Task Test_configured_first_time_checkpoint_is_used() {
    var configured = clock.UtcNow().AddYears(-5);
    var func = new CheckpointRecordingFunction(new(name), repo, clock, configured);
    await F.RunFunc(clock, func, repo);
    
    Assert.That(func.Checkpoints.Single(), Is.EqualTo(configured));
  }
  
  [Test] public void Test_GetReadyOperations_correctly_filters_out_operations_not_meeting_cron_criteria() {
    OperationStateAndConfig<ReadOperationConfig> Op(string opname, string cron, DateTime last) => new(
        new ObjectState(new(opname), new(opname), new SystemEntityTypeName(opname), clock.UtcNow(), true, clock.UtcNow()) { 
          LastCompleted = last,
          LastResult = EOperationResult.Success,
          LastAbortVote = EOperationAbortVote.Continue
        }, 
        F.EmptyFunctionConfig(), 
        new(new SystemEntityTypeName(opname), new (new (cron)), GetListResult), 
        DateTime.MinValue);
    DateTime Dt(string dt) => DateTime.Parse(dt).ToUniversalTime();
    var now = Dt("2024-08-01T01:30:00Z");                   // 01:30 UTC on August 1st, 2024
    
    var ops = new List<OperationStateAndConfig<ReadOperationConfig>> {
      Op("1", "0 0 0 * * *", Dt("2024-07-31T23:59:59Z")),   // Daily at 00:00 UTC, ready for 01:00 UTC
      Op("2", "0 0 * * * *", Dt("2024-08-01T00:00:00Z")),   // Hourly at 00 minutes, ready for 01:00 UTC
      Op("3", "0 0 0 1 * *", Dt("2024-07-01T00:00:00Z")),   // Monthly on the 1st at 00:00 UTC, ready for 2024-08-01
      
      Op("4", "0 0 0 * * *", Dt("2024-08-01T00:00:00Z")),   // Daily at 00:00 UTC, not yet ready at 01:00 UTC
      Op("5", "0 0 0 * * MON", Dt("2024-07-29T00:00:00Z")), // Weekly on Monday at 00:00 UTC, now is not Monday
      Op("6", "0 0 * * * *", Dt("2024-08-01T01:00:00Z"))    // Hourly at 00 minutes, not yet ready
    } ;
    var ready = AbstractFunction<ReadOperationConfig>.GetReadyOperations(ops, [], now); 
    Assert.That(ready.Select(op => op.State.Object.Value), Is.EquivalentTo(["1", "2", "3"]));
  }
  
  [Test] public async Task Test_RunOperationsTillAbort_on_single_valid_op() {
    var runner = F.ReadFunc(clock, ctl: repo);
    
    var states1 = new List<OperationStateAndConfig<ReadOperationConfig>> { await F.CreateReadOpStateAndConf(clock, repo) };
    var res1 = (await RunOps(states1, runner)).Single().Result;
    
    var states2 = new List<OperationStateAndConfig<ReadOperationConfig>> { await F.CreateErroringOpStateAndConf(clock, repo) };
    var res2 = (await RunOps(states2, runner)).Single().Result;
    
    var newstates = repo.Objects.Values.ToList();

    Assert.That(res1, Is.EqualTo(new EmptyReadOperationResult()));
    Assert.That(res2, Is.EqualTo(new ErrorOperationResult(0, EOperationAbortVote.Abort, res2.Exception)));
    
    Assert.That(newstates, Has.Count.EqualTo(2));
    Assert.That(newstates[0], Is.EqualTo(ExpObjState(EOperationResult.Success, EOperationAbortVote.Continue, 0, clock.UtcNow())));
    Assert.That(newstates[1], Is.EqualTo(ExpObjState(EOperationResult.Error, EOperationAbortVote.Abort, 0, clock.UtcNow(), res2.Exception)));
  }

  [Test] public async Task Test_RunOperationsTillAbort_stops_on_first_abort() {
    var runner = F.ReadFunc(clock, ctl: repo);
    
    var states = new List<OperationStateAndConfig<ReadOperationConfig>> {
      await F.CreateErroringOpStateAndConf(clock, repo),
      await F.CreateReadOpStateAndConf(clock, repo)
    };
    
    var res = (await RunOps(states, runner)).Single().Result;
    var resex = res.Exception;
    var newstates = repo.Objects.Values.ToList();
    
    Assert.That(res, Is.EqualTo(new ErrorOperationResult(0, EOperationAbortVote.Abort, resex)));
    
    Assert.That(newstates, Has.Count.EqualTo(2));
    Assert.That(newstates[0], Is.EqualTo(ExpObjState(EOperationResult.Error, EOperationAbortVote.Abort, 0, clock.UtcNow(), resex)));
    Assert.That(newstates[1], Is.EqualTo(states[1].State)); // remained unchanged
  }
  
  [Test] public async Task Test_RunOperationsTillAbort_stops_on_first_exception() {
    var runner = F.ReadFunc(clock, ctl: repo);
    
    var states = new List<OperationStateAndConfig<ReadOperationConfig>> {
      await F.CreateErroringOpStateAndConf(clock, repo),
      await F.CreateReadOpStateAndConf(clock, repo)
    };
    states[0] = states[0] with { OpConfig = states[0].OpConfig with { GetUpdatesAfterCheckpoint = ThrowsError } }; 
    var results = (await RunOps(states, runner)).ToList();
    var newstates = repo.Objects.Values.ToList();
    var ex = results[0].Result.Exception ?? throw new Exception();

    Assert.That(results, Has.Count.EqualTo(1));
    Assert.That(results[0].Result, Is.EqualTo(new ErrorOperationResult(0, EOperationAbortVote.Abort, ex)));
    
    Assert.That(newstates, Has.Count.EqualTo(2));
    var exp2 = ExpObjState(EOperationResult.Error, EOperationAbortVote.Abort, 0, clock.UtcNow(), ex);
    Assert.That(newstates[0], Is.EqualTo(exp2));
    Assert.That(newstates[1], Is.EqualTo(states[1].State)); // remained unchanged
  }
  
  [Test] public async Task Test_RunOperations_does_not_update_NextCheckpoint_on_error() {
    var (startingcp, runner) = (clock.UtcNow().AddMinutes(1), F.ReadFunc(clock, ctl: repo));
    var sysstate = await repo.CreateSystemState(new(name), LifecycleStage.Defaults.Read);
    var objstate = await repo.CreateObjectState(sysstate, new(name), startingcp);
    var readopcfg = new ReadOperationConfig(new(nameof(EOperationResult.Error)), TestingDefaults.CRON_EVERY_SECOND, _ => throw new Exception());
    var opconfigs = new List<OperationStateAndConfig<ReadOperationConfig>> {
      new(objstate, F.EmptyFunctionConfig(), readopcfg, objstate.NextCheckpoint)
    };
    
    clock.Tick();
    await RunOps(opconfigs, runner);
    clock.Tick();
    
    var loaded = await repo.GetObjectState(sysstate, new(name)) ?? throw new Exception();
    Assert.That(loaded.NextCheckpoint, Is.EqualTo(startingcp));
  }
  
  [Test] public async Task Test_RunOperations_sets_NextCheckpoint_to_op_start_when_empty() {
    var (startingcp, runner) = (clock.UtcNow().AddMinutes(1), F.ReadFunc(clock, ctl: repo));
    var sysstate = await repo.CreateSystemState(new(name), LifecycleStage.Defaults.Read);
    var objstate = await repo.CreateObjectState(sysstate, new(name), startingcp);
    var readopcfg = new ReadOperationConfig(new(nameof(EOperationResult.Success)), TestingDefaults.CRON_EVERY_SECOND, F.GetEmptyResult);
    var opconfigs = new List<OperationStateAndConfig<ReadOperationConfig>> {
      new(objstate, F.EmptyFunctionConfig(), readopcfg, objstate.NextCheckpoint)
    };
    
    var opstart = clock.Tick();
    await RunOps(opconfigs, runner);
    clock.Tick();
    
    var loaded = await repo.GetObjectState(sysstate, new(name)) ?? throw new Exception();
    Assert.That(loaded.NextCheckpoint, Is.EqualTo(opstart));
  }
  
  [Test] public async Task Test_RunOperations_sets_NextCheckpoint_to_result_when_non_empty_success() {
    var (startingcp, successcp, runner) = (clock.UtcNow().AddMinutes(1), clock.UtcNow().AddMinutes(2), new EmptyReadFunction(new(name), F.SeRepo(clock), repo, clock));
    var sysstate = await repo.CreateSystemState(new(name), LifecycleStage.Defaults.Read);
    var objstate = await repo.CreateObjectState(sysstate, new(name), startingcp);
    var readopcfg = new ReadOperationConfig(new(nameof(EOperationResult.Success)), TestingDefaults.CRON_EVERY_SECOND, config => GetListResult(config, successcp));
    var opconfigs = new List<OperationStateAndConfig<ReadOperationConfig>> {
      new(objstate, F.EmptyFunctionConfig(), readopcfg, objstate.NextCheckpoint)
    };
    clock.Tick();
    await RunOps(opconfigs, runner);
    clock.Tick();
    
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
      OtherFunctionPollExpression = "*/4 * * * * *",
      AzureDefaultValues = new AzureDefaultValuesSettings {
        AppServiceSkuName = String.Empty,
        AppServiceSkuTier = String.Empty,
        FunctionAppNameTemplate = String.Empty,
        WebSiteNameTemplate = String.Empty,
        AppServicePlanNameTemplate = String.Empty
      }
    };
    Assert.That(((IRunnableFunction) F.ReadFunc(clock)).GetFunctionPollCronExpression(defs), Is.EqualTo(new ValidString("*/1 * * * * *")));
    Assert.That(((IRunnableFunction) F.PromoteFunc(clock)).GetFunctionPollCronExpression(defs), Is.EqualTo(new ValidString("*/2 * * * * *")));
    Assert.That(((IRunnableFunction) F.WriteFunc(clock)).GetFunctionPollCronExpression(defs), Is.EqualTo(new ValidString("*/3 * * * * *")));
  }
  
  [Test] public void Go() {}
  
  private async Task<List<OpResultAndObject>> RunOps(List<OperationStateAndConfig<ReadOperationConfig>> ops, AbstractFunction<ReadOperationConfig> func) {
    var results = new List<OpResultAndObject>();
    await func.RunOperationsTillAbort(ops, results, false);
    return results;
  }

  private ObjectState ExpObjState(EOperationResult res, EOperationAbortVote vote, int len, DateTime nextcheckpoint, Exception? ex = null) {
    return new ObjectState(new(res.ToString()), new(res.ToString()), new SystemEntityTypeName(res.ToString()), nextcheckpoint, true, clock.UtcNow()) {
      DateCreated = clock.UtcNow(),
      LastResult = res, 
      LastAbortVote = vote, 
      DateUpdated = clock.UtcNow(), 
      LastStart = clock.UtcNow(), 
      LastSuccessStart = res == EOperationResult.Success ? clock.UtcNow() : null, 
      LastCompleted = clock.UtcNow(),
      LastSuccessCompleted = res == EOperationResult.Success ? clock.UtcNow() : null,
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
  
  private Task<ReadOperationResult> GetListResult(OperationStateAndConfig<ReadOperationConfig> config) => GetListResult(config, null);
  
  private Task<ReadOperationResult> GetListResult(OperationStateAndConfig<ReadOperationConfig> _, DateTime? nextcheckpoint) => 
      Task.FromResult<ReadOperationResult>(new ListReadOperationResult(Enumerable.Range(0, 100).Select(_ => Guid.NewGuid().ToString()).ToList(), nextcheckpoint ?? clock.UtcNow()));
  
  
  private Task<ReadOperationResult> ThrowsError(OperationStateAndConfig<ReadOperationConfig> _) => throw new Exception();
  
  class CheckpointRecordingFunction(SystemName system, ICtlRepository ctl, TimeProvider clock, DateTime? firstcheckpoint = null) : AbstractFunction<ReadOperationConfig>(system, LifecycleStage.Defaults.Read, ctl, clock) {
    
    public List<DateTime> Checkpoints { get; } = [];
    
    protected override FunctionConfig GetFunctionConfiguration() => new([
      new ReadOperationConfig(C.SystemEntityName, TestingDefaults.CRON_EVERY_SECOND, F.GetEmptyResult)
    ]) { DefaultFirstTimeCheckpoint = firstcheckpoint };

    public override Task<OperationResult> RunOperation(OperationStateAndConfig<ReadOperationConfig> op) {
      Checkpoints.Add(op.Checkpoint);
      return Task.FromResult<OperationResult>(new EmptyReadOperationResult());
    }
  }
}