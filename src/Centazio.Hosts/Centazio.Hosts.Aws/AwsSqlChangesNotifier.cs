using System.Text.Json;
using Centazio.Core;
using Centazio.Core.Runner;

namespace Centazio.Hosts.Aws;

public class AwsSqlChangesNotifier(bool localaws) : IChangesNotifier, IDisposable {

  private SqsMessageBus msgbus = null!;
  private CancellationTokenSource cts = null!;
  private List<IRunnableFunction> funcs = null!;

  public void Init(List<IRunnableFunction> functions) {
    funcs = functions;
    msgbus = new SqsMessageBus(localaws);
    cts = new CancellationTokenSource();
  }

  public async Task Run(IFunctionRunner runner) {
    await msgbus.Initialize();
    _ = Task.Run(async () => {
      await msgbus.StartListening(cts.Token,
          async message => {
            var oct = JsonSerializer.Deserialize<ObjectChangeTrigger>(message.Body);
            if (oct == null) return false;

            var func = funcs.FirstOrDefault(func => func.IsTriggeredBy(oct));
            if (func == null) return false;
            await runner.RunFunction(func, [oct]);
            return true;
          });
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