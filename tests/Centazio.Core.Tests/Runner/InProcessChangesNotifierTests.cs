using Centazio.Core.Promote;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Core.Write;
using Centazio.Test.Lib;

namespace Centazio.Core.Tests.Runner;

public class InProcessChangesNotifierTests {

  private readonly LifecycleStage stage1 = new("stage1");
  private readonly LifecycleStage stage2 = new("stage2");
  private readonly LifecycleStage stage3 = new("stage3");
  
  [Test] public void Test_Triggers_aggregation_works() {
    var triggers = new List<OpChangeTriggerKey> {
      new(C.SystemEntityName, stage2),
      new(C.SystemEntityName, stage3),
      new(new("x"), stage1)
    };
    var func = new Func(new("1"), triggers, []);
    var actual = ((IRunnableFunction) func).Triggers();
    Assert.That(actual, Is.EquivalentTo(triggers));
  }
  
  [Test] public async Task Test_notification_works() {
    var func = new Func(stage2, [new(C.SystemEntityName, new (stage1))], [C.CoreEntityName]);
    
    // todo: this circular dependency is not nice
    var notif = new InProcessChangesNotifier([func]);
    var runner = new Runner(notif);
    _ = notif.InitDynamicTriggers(runner);
    var notifications = 10;
    await Enumerable.Range(0, notifications).Select(async _ => {
      await notif.Notify(stage1, [C.SystemEntityName]);
      await Task.Delay(50);
    }).Synchronous();
    
    Assert.That(func.RunCount, Is.EqualTo(notifications));
  }
  
  [Test] public void Test_overwritting_op_config_triggers_works() {
    var triggers = new List<OpChangeTriggerKey> { new(C.SystemEntityName, stage1) };
    var roc = new ReadOperationConfig(C.SystemEntityName, CronExpressionsHelper.EverySecond(), null!) { Triggers = triggers };
    var poc = new PromoteOperationConfig(typeof(System1Entity), C.SystemEntityName, C.CoreEntityName, CronExpressionsHelper.EverySecond(), null!) { Triggers = triggers };
    var woc = new WriteOperationConfig(C.CoreEntityName, CronExpressionsHelper.EverySecond(), null!, null!) { Triggers = triggers };
    
    Assert.That(roc.Triggers, Is.EquivalentTo(triggers));
    Assert.That(poc.Triggers, Is.EquivalentTo(triggers));
    Assert.That(woc.Triggers, Is.EquivalentTo(triggers));
  }
  
  class Func(LifecycleStage stage, List<OpChangeTriggerKey> triggers, List<ObjectName> result) : IRunnableFunction {
    
    public SystemName System { get; } = C.System1Name;
    public LifecycleStage Stage { get; } = stage; 
    public bool Running { get; } = false;
    public FunctionConfig Config { get; } = new ([
      new ReadOperationConfig(C.SystemEntityName, CronExpressionsHelper.EverySecond(), null!) { Triggers = triggers }  
    ]);
    
    public int RunCount { get; private set; } 
    
    public void Dispose() { throw new Exception(); }
    
    public Task RunFunctionOperations(SystemState sys, List<OpResultAndObject> runningresults) {
      RunCount++;
      return Task.FromResult(result.Select(obj => new OpResultAndObject(obj, ReadOperationResult.EmptyResult())).ToList());
    }

  }
  
  class Runner(IChangesNotifier notif) : IFunctionRunner {

    public async Task<FunctionRunResults> RunFunction(IRunnableFunction func) {
      var results = new List<OpResultAndObject>();
      await func.RunFunctionOperations(SystemState.Create(C.System1Name, func.Stage), results);
      await notif.Notify(func.Stage, results.Select(c => c.Object).Distinct().ToList());
      return new SuccessFunctionRunResults(results);
    }

  }
  
}

