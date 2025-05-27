using System.Text.Json;
using Centazio.Core.Runner;
using Centazio.Hosts.Aws;

namespace Centazio.Core.Tests.Aws;

public class AwsSqsMessageBusTests {
  private readonly LifecycleStage stage1 = new("stage1");
  
  [Test, Ignore("LocalStack is required")] public async Task Test_send_receive_message() {
    var sms = new SqsMessageBus(true);
    await sms.Initialize();
    var oct = new ObjectChangeTrigger(C.System1Name, stage1, C.SystemEntityName);
    await sms.TriggerFunction(oct);

    var cts = new CancellationTokenSource();
    await sms.ReceiveAndProcessMessages(cts.Token,
        message => {
          var msg = JsonSerializer.Deserialize<ObjectChangeTrigger>(message.Body)!;
          Assert.Equals(msg.System, C.System1Name);
          Assert.Equals(msg.Stage, stage1.Value);
          Assert.Equals(msg.Object.Value, C.SystemEntityName.Value);
          return Task.FromResult(true);
        });
  }
}