using Centazio.Core.Runner;
using Centazio.Hosts.Aws;

namespace Centazio.Core.Tests.Aws;

public class AwsSqsMessageBusTests {
  private readonly LifecycleStage stage1 = new("stage1");
  
  [Test, Ignore("LocalStack is required")] public async Task Test_send_receive_message() {
    var sms = new SqsMessageBus($"centazio-test-send_receive_message-{DateTime.Now:yyyyMMddhhmmss}", true);
    await sms.Initialize();
    var oct = new ObjectChangeTrigger(C.System1Name, stage1, C.SystemEntityName);
    await sms.TriggerFunction(oct);
    
    var cts = new CancellationTokenSource();
    await sms.ReceiveAndProcessMessages(cts.Token,
        message => {
          var messageBody = message.Body;
          var msg = Json.Deserialize<ObjectChangeTrigger>(messageBody);
          Assert.That(msg.System.Value, Is.EqualTo(C.System1Name.Value));
          Assert.That(msg.Stage.Value, Is.EqualTo(stage1.Value));
          Assert.That(msg.Object.Value, Is.EqualTo(C.SystemEntityName.Value));
          return Task.FromResult(true);
        });
  }
}