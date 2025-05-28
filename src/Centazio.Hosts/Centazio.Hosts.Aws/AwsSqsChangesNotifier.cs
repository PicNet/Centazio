using System.Text.Json;
using Amazon.SQS.Model;
using Centazio.Core;
using Centazio.Core.Runner;
using Serilog;

namespace Centazio.Hosts.Aws;

public class AwsSqsChangesNotifier(bool localaws) : IChangesNotifier, IDisposable {

  private SqsMessageBus msgbus = null!;
  private CancellationTokenSource cts = null!;
  private List<IRunnableFunction> funcs = null!;

  public void Init(List<IRunnableFunction> functions) {
    funcs = functions;
    msgbus = new SqsMessageBus(SqsMessageBus.DEFAULT_QUEUE_NAME, localaws);
    cts = new CancellationTokenSource();
  }

  public async Task Run(IFunctionRunner runner) {
    await msgbus.Initialize();
    _ = Task.Run(async () => {
      await msgbus.StartListening(cts.Token,
          async message => {
            // TODO skip old messages
            return await ProcessMessage(runner, message);
          });
    });
  }

  private async Task<bool> ProcessMessage(IFunctionRunner runner, Message message) {
    var oct = JsonSerializer.Deserialize<ObjectChangeTrigger>(message.Body);
    if (oct == null) return false;
            
    Log.Information("Received message: System[{System}] Stage[{Stage}] Object[{Object}]", oct.System, oct.Stage, oct.Object);
    var func = funcs.FirstOrDefault(func => func.IsTriggeredBy(oct));
    if (func == null) return false;
    Log.Information("Running function: System[{System}] Stage[{Stage}] Object[{Object}]", oct.System, oct.Stage, oct.Object);
    await runner.RunFunction(func, [oct]);
    return true;
  }

  public Task Notify(SystemName system, LifecycleStage stage, List<ObjectName> objs) {
    var triggers = objs.Distinct().Select(obj => new ObjectChangeTrigger(system, stage, obj)).ToList();

    triggers.ForEach(async void (t) => {
      await msgbus.TriggerFunction(t);
    });
    return Task.CompletedTask;
  }

  public bool IsAsync => true;

  public async void Dispose() {
    await cts.CancelAsync();
    await Task.Delay(1000);
  }

}