﻿using Centazio.Core.Runner;
using System.IO.Compression;
using System.Text;
using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Amazon.Runtime;
using Centazio.Core;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Centazio.Core.Promote;
//using Testcontainers.LocalStack;
using Environment = System.Environment;
using ResourceNotFoundException = Amazon.Lambda.Model.ResourceNotFoundException;

namespace Centazio.Hosts.Aws.Tests;

public class AwsEventBridgeChangesNotifierTests {

  [Test, Ignore("set the test localstack")] public async Task Test_setup_event_bridge() {
    
    // TODO CP somehow when using the following test local stack is not working. Need to fix
    // var localStackContainer = new LocalStackBuilder().Build();
    // await localStackContainer.StartAsync().ConfigureAwait(false);
    // var serverurl = localStackContainer.GetConnectionString();
    
    var serverurl = "http://localhost:4566";
    var lambda = new AmazonLambdaClient(new BasicAWSCredentials("test", "test"), new AmazonLambdaConfig { ServiceURL = serverurl });
    var evbridge = new AmazonEventBridgeClient(new BasicAWSCredentials("test", "test"), new AmazonEventBridgeConfig { ServiceURL = serverurl });
    
    AwsEventBridgeChangesNotifier notifier = new(lambda, evbridge);

    var funcnm = "dummy-target-function";
    try { await DeleteDummyFunction(lambda, funcnm); }
    catch (ResourceNotFoundException) { }

    await CreateDummyLambdaFunction(lambda, funcnm);
    var response = await lambda.InvokeAsync(new InvokeRequest {
      FunctionName = funcnm,
      Payload = Json.Serialize(new { message = "Hello, Lambda!" })
    });

    Assert.That(response.StatusCode, Is.EqualTo(200));
    
    await DeleteEventBusIfExistsAsync(evbridge, AwsEventBridgeChangesNotifier.EVENT_BUS_NAME);

    var func = new DummyRunnableFunction();
    await notifier.Setup(func);
    await notifier.Notify(func.System, LifecycleStage.Defaults.Read, [func.SystemEntityTypeName]);

    //TODO CP check if the lambda is called via the event bridge
  }

  private static async Task DeleteDummyFunction(AmazonLambdaClient lambda, string funcnm) {
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
  print(f""Lambda invoked with event: {event}"")
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

    // Wait until the Lambda function is created
    while (true) {
      try {
        var response = await lambda.GetFunctionAsync(new GetFunctionRequest { FunctionName = funcnm });
        if (response.Configuration.State != "Pending" ) break;
      } catch (ResourceNotFoundException) { await Task.Delay(1000); }
    }

    Environment.SetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME", funcnm);
  }

  private async Task DeleteEventBusIfExistsAsync(AmazonEventBridgeClient evbridge, string evtbusname) {
    try {
      await evbridge.DescribeEventBusAsync(new DescribeEventBusRequest { Name = evtbusname });
      var ruleres = await evbridge.ListRulesAsync(new ListRulesRequest { EventBusName = evtbusname });
      foreach (var rule in ruleres.Rules) {
        var targetsResponse = await evbridge.ListTargetsByRuleAsync(new ListTargetsByRuleRequest { Rule = rule.Name, EventBusName = evtbusname });
        if (targetsResponse.Targets.Any()) { await evbridge.RemoveTargetsAsync(new RemoveTargetsRequest { Rule = rule.Name, EventBusName = evtbusname, Ids = targetsResponse.Targets.Select(target => target.Id).ToList() }); }
        await evbridge.DeleteRuleAsync(new DeleteRuleRequest { Name = rule.Name, EventBusName = evtbusname });
      }
      await evbridge.DeleteEventBusAsync(new DeleteEventBusRequest { Name = evtbusname });
    } catch (ResourceNotFoundException) { }
  }

}

public class DummyRunnableFunction : IRunnableFunction {
  private readonly DummySystemType sysentity = new("DummySystemType", new SystemEntityId("DummySystemType"), UtcDate.UtcNow);
  public readonly SystemEntityTypeName SystemEntityTypeName;

  public DummyRunnableFunction() {
    SystemEntityTypeName = new SystemEntityTypeName("TestSETypNm");
    Config = new FunctionConfig([new PromoteOperationConfig(System, sysentity.GetType(), SystemEntityTypeName, new CoreEntityTypeName("CoreTest"), CronExpressionsHelper.EveryXMinutes(1), (_, _) => Task.FromResult(new List<EntityEvaluationResult>()))]);
  }

  public void Dispose() { }
  public SystemName System { get; } = new("DummyFunction");
  public LifecycleStage Stage => LifecycleStage.Defaults.Read;
  public bool Running => false;
  public FunctionConfig Config { get; }
  public Task RunFunctionOperations(SystemState sys, List<FunctionTrigger> trigger, List<OpResultAndObject> runningresults) { throw new NotImplementedException(); }
  public bool IsTriggeredBy(ObjectChangeTrigger trigger) { throw new NotImplementedException(); }
}

public class DummySystemType(string displayName, SystemEntityId systemId, DateTime lastUpdatedDate) : ISystemEntity {
  public string DisplayName { get; } = displayName;
  public object GetChecksumSubset() => throw new NotImplementedException();
  public SystemEntityId SystemId { get; } = systemId;
  public DateTime LastUpdatedDate { get; } = lastUpdatedDate;
  public ISystemEntity CreatedWithId(SystemEntityId systemid) => throw new NotImplementedException();
}