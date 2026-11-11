using Centazio.Core.Promote;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Core.Write;

namespace Centazio.Core.Tests.Runner;

public class InProcessChangesNotifierTests {

  private readonly LifecycleStage stage1 = new("stage1");
  private readonly LifecycleStage stage2 = new("stage2");
  private readonly LifecycleStage stage3 = new("stage3");
  
  private FakeTimeProvider clock = null!;
  private ICtlRepository ctl = null!;
  
  [SetUp] public void SetUp() {
    clock = F.Clock();
    ctl = F.CtlRepo(clock);
  }
  
  [TearDown] public async Task TearDown() => await ctl.DisposeAsync();

  [Test] public async Task Test_notification_works() {
    var func = new Func(stage2, [new(C.System1Name, stage1, C.SystemEntityName)], ctl, clock);
    
    using var notif = new InProcessChangesNotifier();
    notif.Init([func]);
    var runner = new FunctionRunnerWithNotificationAdapter(new Runner(notif, clock), notif);
    
    var notifications = 10;
    await Enumerable.Range(0, notifications).Select(async _ => {
      clock.Tick();
      await notif.Notify(C.System1Name, stage1, [C.SystemEntityName]);
      await notif.ProcessPending(runner);
    }).Synchronous();
    
    Assert.That(func.RunCount, Is.EqualTo(notifications));
  }
  
  [Test] public async Task Test_ProcessPending_runs_chained_triggers_until_the_queue_is_empty() {
    var func2 = new Func(stage2, [new(C.System1Name, stage1, C.SystemEntityName)], ctl, clock);
    var func3 = new Func(stage3, [new(C.System1Name, stage2, C.SystemEntityName)], ctl, clock);
    
    using var notif = new InProcessChangesNotifier();
    notif.Init([func2, func3]);
    var runner = new FunctionRunnerWithNotificationAdapter(new Runner(notif, clock), notif);
    
    clock.Tick();
    await notif.Notify(C.System1Name, stage1, [C.SystemEntityName]);
    await notif.ProcessPending(runner);
    var (runs2, runs3) = (func2.RunCount, func3.RunCount);
    await notif.ProcessPending(runner);
    
    Assert.That((runs2, runs3), Is.EqualTo((1, 1)));
    Assert.That((func2.RunCount, func3.RunCount), Is.EqualTo((1, 1)));
  }
  
  [Test] public async Task Test_background_Run_loop_delivers_notifications() {
    var ran = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var func = new Func(stage2, [new(C.System1Name, stage1, C.SystemEntityName)], ctl, clock, () => ran.TrySetResult());
    
    var notif = new InProcessChangesNotifier();
    notif.Init([func]);
    // this runner does not notify, the queue is closed below while the loop may still be running the function
    var runner = new FunctionRunnerWithNotificationAdapter(new Runner(notif, clock, false), notif);
    var loop = Task.Run(() => notif.Run(runner));
    
    clock.Tick();
    await notif.Notify(C.System1Name, stage1, [C.SystemEntityName]);
    await ran.Task.WaitAsync(TimeSpan.FromSeconds(5));
    
    notif.Dispose(); // completes the queue, which ends the loop
    await loop.WaitAsync(TimeSpan.FromSeconds(5));
    Assert.That(func.RunCount, Is.EqualTo(1));
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
  
  class Func(LifecycleStage stage, List<ObjectChangeTrigger> triggers, ICtlRepository ctl, TimeProvider clock, Action? onrun = null) : AbstractFunction<ReadOperationConfig>(C.System1Name, stage, ctl, clock) {
    
    public int RunCount { get; private set; }
    
    protected override FunctionConfig GetFunctionConfiguration() => new ([
      new ReadOperationConfig(C.SystemEntityName, CronExpressionsHelper.EverySecond(), null!) { Triggers = triggers }  
    ]);

    public override Task<OperationResult> RunOperation(OperationStateAndConfig<ReadOperationConfig> op) {
      RunCount++;
      onrun?.Invoke();
      return Task.FromResult<OperationResult>(ReadOperationResult.EmptyResult());
    }
  }
  
  // notifies every object it ran (even when nothing changed) so triggers can be chained without staging data 
  class Runner(IChangesNotifier notif, TimeProvider clock, bool notify = true) : IFunctionRunner {

    public async Task<FunctionRunResults> RunFunction(IRunnableFunction func, List<FunctionTrigger> triggers) {
      var results = new List<OpResultAndObject>();
      await func.RunFunctionOperations(SystemState.Create(C.System1Name, func.Stage, clock.UtcNow()), triggers, results, true);
      if (notify) await notif.Notify(func.System, func.Stage, results.Select(c => c.Object).Distinct().ToList());
      return new SuccessFunctionRunResults(results);
    }

  }
  
}
