using Centazio.Core;
using centazio.core.Ctl;
using Centazio.Core.Func;
using F = centazio.core.tests.Read.ReadTestFactories;

namespace centazio.core.tests.Read;

public class ReadFunctionBaseHelperExtensionsTests {

  private const string NAME = nameof(ReadFunctionBaseHelperExtensionsTests);
  private ICtlRepository repo;
  
  [SetUp] public void SetUp() => repo = new InMemoryCtlRepository();
  [TearDown] public async Task TearDown() => await repo.DisposeAsync();

  [Test] public void Test_ReadFunctionConfig_Validate_fails_with_empty_operations() {
    Assert.Throws<Exception>(() => new ReadFunctionConfig(NAME, NAME, []).Validate());
  }
  
  [Test] public async Task Test_LoadOperationsStates_creates_missing_operations() {
    var ss = await repo.CreateSystemState(NAME, NAME);
    var obj2 = await repo.CreateObjectState(ss, "2");
    
    var cfg = new ReadFunctionConfig(NAME, NAME, Factories.OP_CONFIGS);
    var states = await cfg.Operations.LoadOperationsStates(ss, repo);
    Assert.That(states, Has.Count.EqualTo(4));
    Enumerable.Range(0, 4).ForEachIdx(TestAtIndex);
    
    async void TestAtIndex(int idx) {
      var name = (idx + 1).ToString();
      Assert.That(states[idx].State, Is.EqualTo(idx == 1 ? obj2 : obj2 with { Object = name}));
      Assert.That(states[idx].State, Is.EqualTo(await repo.GetObjectState(ss, name)));
    }
  }
  
  [Test] public async Task Test_LoadOperationsStates_ignores_innactive_states() {
    var ss = await repo.CreateSystemState(NAME, NAME);
    await repo.SaveObjectState(await repo.CreateObjectState(ss, "2") with { Active = false });
    
    var states = await Factories.OP_CONFIGS.LoadOperationsStates(ss, repo);
    var names = states.Select(s => s.Settings.Object.Value).ToList();
    Assert.That(names, Is.EquivalentTo(new [] {"1", "3", "4"}));
  }
  
  [Test] public void Test_GetReadyOperations_correctly_filters_out_operations_not_meeting_cron_criteria() {
    ReadOperationStateAndConfig Op(string name, string cron, DateTime last) => new(new(name, name, name, true, UtcDate.Utc.Now, LastCompleted: last), new(name, new (cron), F.TestingListReadOperationImplementation));
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
  
  static class Factories {
    public static List<ReadOperationConfig> OP_CONFIGS => [
      new ReadOperationConfig("1", new ("* * * * *"), F.TestingEmptyReadOperationImplementation),
      new ReadOperationConfig("2", new ("* * * * *"), F.TestingEmptyReadOperationImplementation),
      new ReadOperationConfig("3", new ("* * * * *"), F.TestingEmptyReadOperationImplementation),
      new ReadOperationConfig("4", new ("* * * * *"), F.TestingEmptyReadOperationImplementation)
    ];
  }
}