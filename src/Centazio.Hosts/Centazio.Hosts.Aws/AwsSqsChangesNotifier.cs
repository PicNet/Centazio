using System.Text.Json;
using Centazio.Core;
using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Serilog;

namespace Centazio.Hosts.Aws;

public class AwsSqsChangesNotifier(bool localaws) : IChangesNotifier, IDisposable {

  private AwsSqsMessageBus msgbus = null!;
  private CancellationTokenSource cts = null!;
  private List<IRunnableFunction> funcs = null!;

  public void Init(List<IRunnableFunction> functions) {
    funcs = functions;
    msgbus = new AwsSqsMessageBus(AwsSqsMessageBus.DEFAULT_QUEUE_NAME, localaws);
    cts = new CancellationTokenSource();
  }

  public async Task Run(IFunctionRunner runner) {
    await msgbus.Initialize();

    await msgbus.StartListening(cts.Token,
        message => {
          var oct = Json.Deserialize<ObjectChangeTrigger>(message.Body);

          Log.Information("Received message: System[{System}] Stage[{Stage}] Object[{Object}]", oct.System, oct.Stage, oct.Object);

          // TODO skip old messages
          funcs.Where(func => func.IsTriggeredBy(oct))
              .ToList()
              .ForEach(async void (func) => {
                await runner.RunFunction(func, [oct]);
              });
          return Task.FromResult(true);
        });
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