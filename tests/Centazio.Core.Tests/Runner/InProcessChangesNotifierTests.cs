using Centazio.Core.Promote;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Core.Write;
using Centazio.Test.Lib;

namespace Centazio.Core.Tests.Runner;

public class InProcessChangesNotifierTests {

  private readonly LifecycleStage stage1 = new("stage1");
  private readonly LifecycleStage stage2 = new("stage2");

  [Test] public async Task Test_notification_works() {
    var func = new Func(stage2, [new(C.System1Name, new (stage1), C.SystemEntityName)]);
    
    var notif = new InProcessChangesNotifier();
    notif.Init([func]);
    var runner = new FunctionRunnerWithNotificationAdapter(new Runner(notif), notif, () => {});
    _ = notif.Run(runner);
    
    var notifications = 10;
    await Enumerable.Range(0, notifications).Select(async _ => {
      await notif.Notify(C.System1Name, stage1, [C.SystemEntityName]);
      TestingUtcDate.DoTick();
      await Task.Delay(50);
    }).Synchronous();
    
    Assert.That(func.RunCount, Is.EqualTo(notifications));
  }
  
  [Test] public void Test_overwritting_op_config_triggers_works() {
    var triggers = new List<ObjectChangeTrigger> { new(C.System1Name, stage1, C.SystemEntityName) };
    var roc = new ReadOperationConfig(C.SystemEntityName, CronExpressionsHelper.EverySecond(), null!) { Triggers = triggers };
    var poc = new PromoteOperationConfig(C.System1Name, typeof(System1Entity), C.SystemEntityName, C.CoreEntityName, CronExpressionsHelper.EverySecond(), null!) { Triggers = triggers };
    var woc = new WriteOperationConfig(C.System1Name, C.CoreEntityName, CronExpressionsHelper.EverySecond(), null!, null!) { Triggers = triggers };
    
    Assert.That(roc.Triggers, Is.EquivalentTo(triggers));
    Assert.That(poc.Triggers, Is.EquivalentTo(triggers));
    Assert.That(woc.Triggers, Is.EquivalentTo(triggers));
  }
  
  class Func(LifecycleStage stage, List<ObjectChangeTrigger> triggers) : AbstractFunction<ReadOperationConfig>(C.System1Name, stage, F.CtlRepo()) {
    
    public int RunCount { get; private set; }
    
    protected override FunctionConfig GetFunctionConfiguration() => new ([
      new ReadOperationConfig(C.SystemEntityName, CronExpressionsHelper.EverySecond(), null!) { Triggers = triggers }  
    ]);

    public override Task<OperationResult> RunOperation(OperationStateAndConfig<ReadOperationConfig> op) {
      RunCount++;
      return Task.FromResult<OperationResult>(ReadOperationResult.EmptyResult());
    }
  }
  
  class Runner(IChangesNotifier notif) : IFunctionRunner {

    public bool Running { get; private set; }

    public async Task<FunctionRunResults> RunFunction(IRunnableFunction func, List<FunctionTrigger> triggers) {
      Running = true;
      var results = new List<OpResultAndObject>();
      await func.RunFunctionOperations(SystemState.Create(C.System1Name, func.Stage), triggers, results);
      await notif.Notify(func.System, func.Stage, results.Select(c => c.Object).Distinct().ToList());
      Running = false;
      return new SuccessFunctionRunResults(results);
    }

  }
  
}

