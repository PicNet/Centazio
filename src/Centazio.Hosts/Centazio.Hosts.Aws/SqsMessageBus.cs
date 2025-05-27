using System.Text.Json;
using Amazon.SQS;
using Amazon.SQS.Model;
using Centazio.Core.Runner;
using Serilog;

namespace Centazio.Hosts.Aws;

public class SqsMessageBus(bool useLocalStack = false) {

  private readonly IAmazonSQS sqs = new AmazonSQSClient(new AmazonSQSConfig {
    ServiceURL = useLocalStack ? "http://localhost:4566" : null
  });

  private string queueurl = null!;

  public async Task Initialize() {
    var res = await sqs.CreateQueueAsync(new CreateQueueRequest {
      QueueName = "centazio-function-triggers",
      Attributes = new Dictionary<string, string> {
        [QueueAttributeName.ReceiveMessageWaitTimeSeconds] = "20",
        [QueueAttributeName.MessageRetentionPeriod] = "1209600",
        [QueueAttributeName.VisibilityTimeout] = "30"
      }
    });
    queueurl = res.QueueUrl;
  }

  public async Task TriggerFunction(ObjectChangeTrigger oct) {
    Log.Information("Triggering function: {System} {Stage} {Object}", oct.System, oct.Stage, oct.Object);
    await sqs.SendMessageAsync(new SendMessageRequest {
      QueueUrl = queueurl,
      MessageBody = JsonSerializer.Serialize(oct)
    });
  }

  public async Task StartListening(CancellationToken cancellationToken, Func<Message, Task<bool>> processMessage) {
    while (!cancellationToken.IsCancellationRequested) {
      var response = await sqs.ReceiveMessageAsync(new ReceiveMessageRequest {
            QueueUrl = queueurl,
            MaxNumberOfMessages = 10,
            WaitTimeSeconds = 20
          },
          cancellationToken);

      foreach (var message in response.Messages) {
        try {
          Log.Information("Processing message: {MessageId}", message.MessageId);
          var success = await processMessage.Invoke(message);
          if (success) await sqs.DeleteMessageAsync(queueurl, message.ReceiptHandle, cancellationToken);
        }
        catch (Exception ex) {
          // If processing fails, the message will become visible again after visibility timeout
          Log.Error(ex, "Error processing message: {MessageId}", message.MessageId);
        }
      }
    }
  }
}