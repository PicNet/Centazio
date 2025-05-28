using Amazon.SQS;
using Amazon.SQS.Model;
using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Serilog;

namespace Centazio.Hosts.Aws;

public class AwsSqsMessageBus(string name, bool useLocalStack = false) {
  public const string DEFAULT_QUEUE_NAME = "centazio-function-triggers"; 
      
  private readonly IAmazonSQS sqs = new AmazonSQSClient(new AmazonSQSConfig { ServiceURL = useLocalStack ? "http://localhost:4566" : null });
  private string queueurl = null!;

  public async Task Initialize() {
    if (useLocalStack) {
      Environment.SetEnvironmentVariable("AWS_ACCESS_KEY_ID", "test");
      Environment.SetEnvironmentVariable("AWS_SECRET_ACCESS_KEY", "test");
      Environment.SetEnvironmentVariable("AWS_DEFAULT_REGION", "ap-southeast-2");
    }

    queueurl = (await sqs.CreateQueueAsync(new CreateQueueRequest {
      QueueName = name,
      Attributes = new Dictionary<string, string> {
        [QueueAttributeName.ReceiveMessageWaitTimeSeconds] = "20",
        [QueueAttributeName.MessageRetentionPeriod] = "1209600",
        [QueueAttributeName.VisibilityTimeout] = "30"
      }
    })).QueueUrl;
  }

  public async Task TriggerFunction(ObjectChangeTrigger oct) {
    Log.Information("Triggering function: System[{System}] Stage[{Stage}] Object[{Object}]", oct.System, oct.Stage, oct.Object);
    await sqs.SendMessageAsync(new SendMessageRequest { QueueUrl = queueurl, MessageBody = Json.Serialize(oct) });
  }

  public async Task StartListening(CancellationToken cancellationToken, Func<Message, Task<bool>> processMessage) {
    while (!cancellationToken.IsCancellationRequested) { await ReceiveAndProcessMessages(cancellationToken, processMessage); }
  }

  public async Task ReceiveAndProcessMessages(CancellationToken cancellationToken, Func<Message, Task<bool>> processMessage) {
    var response = await sqs.ReceiveMessageAsync(new ReceiveMessageRequest { QueueUrl = queueurl, MaxNumberOfMessages = 10, WaitTimeSeconds = 20 }, cancellationToken);
    
    response.Messages.ForEach(async void (message) => {
      try {
        Log.Information("Processing message: {MessageId}", message.MessageId);
        if (await processMessage.Invoke(message)) await sqs.DeleteMessageAsync(queueurl, message.ReceiptHandle, cancellationToken);
      }
      catch (Exception ex) {
        Log.Error(ex, "Error processing message: message id[{MessageId}] message[{Message}] ", message.MessageId, ex.Message);
      }
    });
  }

}