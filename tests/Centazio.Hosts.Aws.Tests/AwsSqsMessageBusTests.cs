using Centazio.Core;
using Centazio.Core.Misc;
using Centazio.Core.Runner;

namespace Centazio.Hosts.Aws.Tests;

public class AwsSqsMessageBusTests {
  private readonly LifecycleStage stage1 = new("stage1");
  
  // todo CP: please use https://docs.localstack.cloud/user-guide/integrations/testcontainers/
  [Test, Ignore("localstack required to test this")] public async Task Test_send_receive_message() {
    var sms = new AwsSqsMessageBus($"centazio-test-send_receive_message-{UtcDate.UtcNow:yyyyMMddhhmmss}", true);
    await sms.Initialize();
    var oct = new ObjectChangeTrigger(Test.Lib.Constants.System1Name, stage1, Test.Lib.Constants.SystemEntityName);
    await sms.TriggerFunction(oct);
    
    var cts = new CancellationTokenSource();
    await sms.ReceiveAndProcessMessages(cts.Token,
        message => {
          var messageBody = message.Body;
          var msg = Json.Deserialize<ObjectChangeTrigger>(messageBody);
          Assert.That(msg.System.Value, Is.EqualTo(Test.Lib.Constants.System1Name.Value));
          Assert.That(msg.Stage.Value, Is.EqualTo(stage1.Value));
          Assert.That(msg.Object.Value, Is.EqualTo(Test.Lib.Constants.SystemEntityName.Value));
          return Task.FromResult(true);
        });
  }
}