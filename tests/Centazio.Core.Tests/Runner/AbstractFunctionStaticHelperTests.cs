﻿using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Test.Lib;

namespace Centazio.Core.Tests.Runner;

public class AbstractFunctionStaticHelperTests {

  private const string NAME = nameof(AbstractFunctionStaticHelperTests);
  private TestingCtlRepository repo;
  private IObjectStateRepo<ExternalEntityType> objrepo;
  
  [SetUp] public void SetUp() {
    repo = TestingFactories.CtlRepo();
    objrepo = repo.GetObjectStateRepo<ExternalEntityType>();
  }

  [TearDown] public async Task TearDown() {
    await objrepo.DisposeAsync();
    await repo.DisposeAsync();
  }

  [Test] public void Test_ReadFunctionConfig_Validate_fails_with_empty_operations() {
    Assert.Throws<ArgumentException>(() => _ = new FunctionConfig<ReadOperationConfig, ExternalEntityType>(NAME, NAME, new ([])));
  }
  
  [Test] public async Task Test_LoadOperationsStates_creates_missing_operations() {
    var ss = await repo.CreateSystemState(NAME, NAME);
    var template = await objrepo.CreateObjectState(ss, new ExternalEntityType("2")); 
    
    var cfg = new FunctionConfig<ReadOperationConfig, ExternalEntityType>(NAME, NAME, Factories.READ_OP_CONFIGS);
    var states = await AbstractFunction<ReadOperationConfig, ExternalEntityType, ReadOperationResult>.LoadOperationsStates(cfg, ss, repo);
    
    Assert.That(states, Has.Count.EqualTo(4));
    Enumerable.Range(0, 4).ForEach(TestAtIndex);
    
    async void TestAtIndex(int idx) {
      var name = (idx + 1).ToString();
      var exp = (ObjectState<ExternalEntityType>.Dto.FromObjectState(template) with { Object = new ExternalEntityType(name) }).ToObjectState<ExternalEntityType>();
      var actual = states[idx].State;
      
      Assert.That(actual, Is.EqualTo(exp));
      Assert.That(actual, Is.EqualTo(await objrepo.GetObjectState(ss, new ExternalEntityType(name))));
    }
  }
  
  [Test] public async Task Test_LoadOperationsStates_ignores_innactive_states() {
    var ss = await repo.CreateSystemState(NAME, NAME);
    var updated = (await objrepo.CreateObjectState(ss, new ExternalEntityType("2"))).SetActive(false);
    await objrepo.SaveObjectState(updated);
    
    var config = new FunctionConfig<ReadOperationConfig, ExternalEntityType>(NAME, NAME, Factories.READ_OP_CONFIGS);
    var states = await AbstractFunction<ReadOperationConfig, ExternalEntityType, ReadOperationResult>.LoadOperationsStates(config, ss, repo);
    var names = states.Select(s => s.Config.Object.Value).ToList();
    Assert.That(names, Is.EquivalentTo(new [] {"1", "3", "4"}));
  }
  
  [Test] public void Test_GetReadyOperations_correctly_filters_out_operations_not_meeting_cron_criteria() {
    OperationStateAndConfig<ReadOperationConfig, ExternalEntityType> Op(string name, string cron, DateTime last) => new(new ObjectState<ExternalEntityType>.Dto(name, name, new ExternalEntityType(name), true) { 
      LastCompleted = last,
      LastResult = EOperationResult.Success.ToString(),
      LastAbortVote = EOperationAbortVote.Continue.ToString()
    }.ToObjectState<ExternalEntityType>(), new(new ExternalEntityType(name), new (new (cron)), new TestingListReadOperationImplementation()));
    DateTime Dt(string dt) => DateTime.Parse(dt).ToUniversalTime();

    using var _ = new ShortLivedUtcDateOverride(
        Dt("2024-08-01T01:30:00Z"));                        // 01:30 UTC on August 1st, 2024
    
    var ops = new [] {
      Op("1", "0 0 0 * * *", Dt("2024-07-31T23:59:59Z")),   // Daily at 00:00 UTC, ready for 01:00 UTC
      Op("2", "0 0 * * * *", Dt("2024-08-01T00:00:00Z")),   // Hourly at 00 minutes, ready for 01:00 UTC
      Op("3", "0 0 0 1 * *", Dt("2024-07-01T00:00:00Z")),   // Monthly on the 1st at 00:00 UTC, ready for 2024-08-01
      
      Op("4", "0 0 0 * * *", Dt("2024-08-01T00:00:00Z")),   // Daily at 00:00 UTC, not yet ready at 01:00 UTC
      Op("5", "0 0 0 * * MON", Dt("2024-07-29T00:00:00Z")), // Weekly on Monday at 00:00 UTC, now is not Monday
      Op("6", "0 0 * * * *", Dt("2024-08-01T01:00:00Z"))    // Hourly at 00 minutes, not yet ready
    } ;
    var ready = AbstractFunction<ReadOperationConfig, ExternalEntityType, ReadOperationResult>.GetReadyOperations(ops); 
    Assert.That(ready.Select(op => op.State.Object.Value), Is.EquivalentTo(new [] { "1", "2", "3" }));
  }
  
  [Test] public async Task Test_RunOperationsTillAbort_on_single_valid_op() {
    var runner = TestingFactories.ReadRunner();
    
    var states1 = new List<OperationStateAndConfig<ReadOperationConfig, ExternalEntityType>> { await Factories.CreateReadOpStateAndConf(EOperationResult.Success, repo) };
    var results1 = await AbstractFunction<ReadOperationConfig, ExternalEntityType, ReadOperationResult>.RunOperationsTillAbort(states1, runner, repo);
    
    var states2 = new List<OperationStateAndConfig<ReadOperationConfig, ExternalEntityType>> { await Factories.CreateReadOpStateAndConf(EOperationResult.Error, repo) };
    var results2 = await AbstractFunction<ReadOperationConfig, ExternalEntityType, ReadOperationResult>.RunOperationsTillAbort(states2, runner, repo);
    
    var newstates = repo.GetObjects<ExternalEntityType>().Values.ToList();

    Assert.That(results1, Is.EquivalentTo(new [] { new EmptyReadOperationResult() }));
    Assert.That(results2, Is.EquivalentTo(new [] { new ErrorReadOperationResult(EOperationAbortVote.Abort) }));
    
    Assert.That(newstates, Has.Count.EqualTo(2));
    Assert.That(newstates[0], Is.EqualTo(ExpObjState(EOperationResult.Success, EOperationAbortVote.Continue, 0)));
    Assert.That(newstates[1], Is.EqualTo(ExpObjState(EOperationResult.Error, EOperationAbortVote.Abort, 0)));
  }

  [Test] public async Task Test_RunOperationsTillAbort_stops_on_first_abort() {
    var runner = TestingFactories.ReadRunner();
    
    var states = new List<OperationStateAndConfig<ReadOperationConfig, ExternalEntityType>> {
      await Factories.CreateReadOpStateAndConf(EOperationResult.Error, repo),
      await Factories.CreateReadOpStateAndConf(EOperationResult.Success, repo)
    };
    
    var results = await AbstractFunction<ReadOperationConfig, ExternalEntityType, ReadOperationResult>.RunOperationsTillAbort(states, runner, repo);
    var newstates = repo.GetObjects<ExternalEntityType>().Values.ToList();
    
    Assert.That(results, Is.EquivalentTo(new [] { 
      new ErrorReadOperationResult(EOperationAbortVote.Abort)
    }));
    
    Assert.That(newstates, Has.Count.EqualTo(2));
    Assert.That(newstates[0], Is.EqualTo(ExpObjState(EOperationResult.Error, EOperationAbortVote.Abort, 0)));
    Assert.That(newstates[1], Is.EqualTo(states[1].State)); // remained unchanged
  }
  
  [Test] public async Task Test_RunOperationsTillAbort_stops_on_first_exception() {
    
    var runner = TestingFactories.ReadRunner();
    
    var states = new List<OperationStateAndConfig<ReadOperationConfig, ExternalEntityType>> {
      await Factories.CreateReadOpStateAndConf(EOperationResult.Error, repo),
      await Factories.CreateReadOpStateAndConf(EOperationResult.Success, repo)
    };
    states[0] = states[0] with { Config = states[0].Config with { GetObjectsToStage = new ErrorReadOperationImplementation() } }; 
    var results = (await AbstractFunction<ReadOperationConfig, ExternalEntityType, ReadOperationResult>.RunOperationsTillAbort(states, runner, repo, false)).ToList();
    var newstates = repo.GetObjects<ExternalEntityType>().Values.ToList();
    var ex = results[0].Exception ?? throw new Exception();

    Assert.That(results, Has.Count.EqualTo(1));
    Assert.That(results[0], Is.EqualTo(new ErrorReadOperationResult(EOperationAbortVote.Abort, ex)));
    
    Assert.That(newstates, Has.Count.EqualTo(2));
    var exp2 = ExpObjState(EOperationResult.Error, EOperationAbortVote.Abort, 0, ex.Message);
    // todo:is this From/ToObject state really required?
    Assert.That(newstates[0], Is.EqualTo((ObjectState<ExternalEntityType>.Dto.FromObjectState(exp2) with { LastRunException = ex.ToString() }).ToObjectState<ExternalEntityType>()));
    Assert.That(newstates[1], Is.EqualTo(states[1].State)); // remained unchanged
  }
  
  private ObjectState<ExternalEntityType> ExpObjState(EOperationResult res, EOperationAbortVote vote, int len, string exmessage="na") {
    return new ObjectState<ExternalEntityType>(res.ToString(), res.ToString(), new ExternalEntityType(res.ToString()), true) {
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
              : "")
    };
  }

  static class Factories {
    public static async Task<OperationStateAndConfig<ReadOperationConfig, ExternalEntityType>> CreateReadOpStateAndConf(EOperationResult result, ICtlRepository repo) 
        => new (
            await repo.GetObjectStateRepo<ExternalEntityType>().CreateObjectState(await repo.CreateSystemState(result.ToString(), result.ToString()), new ExternalEntityType(result.ToString())), 
            new (new ExternalEntityType(result.ToString()), new (new (TestingDefaults.CRON_EVERY_SECOND)), new TestingAbortingAndEmptyReadOperationImplementation()));
    
    public static ValidList<ReadOperationConfig> READ_OP_CONFIGS => new ([
      new ReadOperationConfig(new ExternalEntityType("1"), new (new (TestingDefaults.CRON_EVERY_SECOND)), new TestingEmptyReadOperationImplementation()),
      new ReadOperationConfig(new ExternalEntityType("2"), new (new (TestingDefaults.CRON_EVERY_SECOND)), new TestingEmptyReadOperationImplementation()),
      new ReadOperationConfig(new ExternalEntityType("3"), new (new (TestingDefaults.CRON_EVERY_SECOND)), new TestingEmptyReadOperationImplementation()),
      new ReadOperationConfig(new ExternalEntityType("4"), new (new (TestingDefaults.CRON_EVERY_SECOND)), new TestingEmptyReadOperationImplementation())
    ]);
  }
  
  private class TestingListReadOperationImplementation : IGetObjectsToStage {
    public Task<ReadOperationResult> GetObjects(OperationStateAndConfig<ReadOperationConfig, ExternalEntityType> config) {
      var result = Enum.Parse<EOperationResult>(config.Config.Object); 
      ReadOperationResult res = result == EOperationResult.Error ? new ErrorReadOperationResult() : new ListRecordsReadOperationResult(Enumerable.Range(0, 100).Select(_ => Guid.NewGuid().ToString()).ToList());
      return Task.FromResult(res); 
    }
  }
  
  private class TestingAbortingAndEmptyReadOperationImplementation : IGetObjectsToStage {
    public Task<ReadOperationResult> GetObjects(OperationStateAndConfig<ReadOperationConfig, ExternalEntityType> config) {
      var result = Enum.Parse<EOperationResult>(config.Config.Object);
      ReadOperationResult res = result == EOperationResult.Error ? new ErrorReadOperationResult(EOperationAbortVote.Abort) : new EmptyReadOperationResult(); 
      return Task.FromResult(res);
    }
  }
  
  private class TestingEmptyReadOperationImplementation : IGetObjectsToStage {
    public Task<ReadOperationResult> GetObjects(OperationStateAndConfig<ReadOperationConfig, ExternalEntityType> config) {
      var result = Enum.Parse<EOperationResult>(config.Config.Object);
      ReadOperationResult res = result == EOperationResult.Error ? new ErrorReadOperationResult() : new EmptyReadOperationResult();
      return Task.FromResult(res);
    }
  }
  
  private class ErrorReadOperationImplementation : IGetObjectsToStage {

    public Task<ReadOperationResult> GetObjects(OperationStateAndConfig<ReadOperationConfig, ExternalEntityType> config) => throw new Exception();

  }
}