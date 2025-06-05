using Centazio.Core.Runner;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.Runtime;
using Centazio.Core;
using Centazio.Core.Ctl.Entities;
using Environment = System.Environment;

namespace Centazio.Hosts.Aws.Tests;

public class AwsEventBridgeChangesNotifierTests {

  [Test, Ignore("Not completed yet")] public async Task Test_setup_event_bridge() {
    AwsEventBridgeChangesNotifier notifier = new(true);

    var funcnm = "dummy-target-function";
    var lambda = new AmazonLambdaClient(new BasicAWSCredentials("test", "test"),
        new AmazonLambdaConfig { ServiceURL = "http://localhost:4566" });

    try { await DeleteDummyFunction(lambda, funcnm); }
    catch (ResourceNotFoundException) { }

    await CreateDummyLambdaFunction(lambda, funcnm);

    await Task.Delay(10000); // 10 seconds delay

    var response = await lambda.InvokeAsync(new InvokeRequest {
      FunctionName = funcnm,
      Payload = JsonSerializer.Serialize(new { message = "Hello, Lambda!" })
    });

    Assert.That(response.StatusCode, Is.EqualTo(200));

    var func = new DummyRunnableFunction();
    await notifier.Setup(func);
    await notifier.Notify(func.System, func.Stage, [new("Dummy")]);

    //TODO check if the lambda is called via the event bridge
  }

  private static async Task DeleteDummyFunction(AmazonLambdaClient lambda, string funcnm)
  {
    await lambda.GetFunctionAsync(new GetFunctionRequest { FunctionName = funcnm });
    await lambda.DeleteFunctionAsync(new DeleteFunctionRequest { FunctionName = funcnm });
  }

  private static async Task CreateDummyLambdaFunction(AmazonLambdaClient lambda, string funcnm) {
    byte[] zipFileBytes;
    using (var memoryStream = new MemoryStream()) {
      using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true)) {
        var entry = archive.CreateEntry("lambda_function.py");
        await using (var entryStream = entry.Open())
        await using (var streamWriter = new StreamWriter(entryStream, Encoding.UTF8)) {
          streamWriter.Write(@"
def lambda_handler(event, context):
    message = event.get('message', 'No message provided')
    return { 'statusCode': 200, 'message': f'Received: {message}' }
");
        }
      }
      zipFileBytes = memoryStream.ToArray();
    }

    await lambda.CreateFunctionAsync(new CreateFunctionRequest {
      FunctionName = funcnm,
      Handler = "lambda_function.lambda_handler",
      Role = "arn:aws:iam::123456789012:role/lambda-role",
      Runtime = "python3.9",
      Code = new FunctionCode {
        ZipFile = new MemoryStream(zipFileBytes)
      }
    });

    Environment.SetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME", funcnm);
  }

}

public class DummyRunnableFunction : IRunnableFunction {
  
  public void Dispose() { }
  public SystemName System { get; } = new("DummyFunction");
  public LifecycleStage Stage { get; } = LifecycleStage.Defaults.Promote;
  public bool Running => false;
  public FunctionConfig Config { get; } = new FunctionConfig(new List<OperationConfig>());

  public Task RunFunctionOperations(SystemState sys, List<FunctionTrigger> trigger, List<OpResultAndObject> runningresults) {
    throw new NotImplementedException();
  }

  public bool IsTriggeredBy(ObjectChangeTrigger trigger) {
    throw new NotImplementedException();
  }

}