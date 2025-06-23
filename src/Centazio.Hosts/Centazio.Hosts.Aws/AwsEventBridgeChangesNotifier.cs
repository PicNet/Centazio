using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Centazio.Core;
using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Serilog;
using Environment = System.Environment;
using ResourceNotFoundException = Amazon.EventBridge.Model.ResourceNotFoundException;

namespace Centazio.Hosts.Aws;

public class AwsEventBridgeChangesNotifier(AmazonLambdaClient lambdaclient, AmazonEventBridgeClient eventbridge) : IChangesNotifier {

  private string? funcnm;
  public const string SOURCE_NAME = "centazio";
  public const string EVENT_BUS_NAME = "centazio-event-bus";
  public const string ENV_SETUP = "ENV_SETUP";
  
  // todo GT: this should not be mandatory as it should only be used for testing, as such should be removed from main interface/absrtact class
  public bool Running { get; }
  
  public void Init(List<IRunnableFunction> functions) { }
  public Task Run(IFunctionRunner runner) => throw new NotImplementedException();

  public async Task Notify(SystemName system, LifecycleStage stage, List<ObjectName> objs) {
    var req = new PutEventsRequest {
      Entries = objs.Select(obj => new PutEventsRequestEntry {
        Source = SOURCE_NAME,
        DetailType = nameof(ObjectChangeTrigger),
        Detail = Json.Serialize(new {
          System = system.Value.ToLower(),
          Stage = stage.Value.ToLower(),
          Object = obj.Value.ToLower()
        }),
        EventBusName = EVENT_BUS_NAME
      }).ToList()
    };

    var res = await eventbridge.PutEventsAsync(req);
    res.Entries.ForEach(result => {
      Log.Information(!string.IsNullOrEmpty(result.EventId) ? $"Event published successfully. Event ID: {result.EventId}" : $"Failed to publish event. Error: {result.ErrorMessage}");
    });
  }

  public async Task Setup(IRunnableFunction func) {
    if (Environment.GetEnvironmentVariable(ENV_SETUP) is "1") return;

    funcnm = Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME");
    if (string.IsNullOrEmpty(funcnm)) throw new InvalidOperationException("Function name not found in environment variables.");
    
    var octs = func.Config.Operations.SelectMany(o => o.Triggers).ToList();
    if (octs.Count <= 0) return;

    var res = await lambdaclient.GetFunctionAsync(new GetFunctionRequest { FunctionName = funcnm });
    await SetupEventBridge(lambdaclient, res.Configuration.FunctionArn, octs);
    await SetEnvironmentVariable(lambdaclient, ENV_SETUP, "1");
  }

  private async Task SetEnvironmentVariable(AmazonLambdaClient lambda, string ekey, string evar) {
    try {
      var cres = await lambda.GetFunctionConfigurationAsync(new GetFunctionConfigurationRequest { FunctionName = funcnm });
      var evars = cres.Environment?.Variables ?? new Dictionary<string, string>();
      evars[ekey] = evar;

      await lambda.UpdateFunctionConfigurationAsync(new UpdateFunctionConfigurationRequest { FunctionName = funcnm, Environment = new Amazon.Lambda.Model.Environment { Variables = evars } });
      Log.Information("Environment variables updated successfully.");
    } catch (Exception ex) {
      Log.Error($"Failed to update environment variables: {ex.Message}");
    }
  }

  private async Task SetupEventBridge(AmazonLambdaClient lambda, string funcarn, List<ObjectChangeTrigger> octs) {
    await CreateOrUpdateEventBusAsync(eventbridge);
    await Task.WhenAll(octs.Select(trigger => CreateEventBridgeRule(lambda, eventbridge, funcarn, trigger)));
  }

  private static async Task CreateOrUpdateEventBusAsync(AmazonEventBridgeClient evbridge) {
    try {
      await evbridge.DescribeEventBusAsync(new DescribeEventBusRequest { Name = EVENT_BUS_NAME });
      Log.Information($"Event bus {EVENT_BUS_NAME} already exists.");
    } catch (ResourceNotFoundException) {
      Log.Information($"Creating event bus {EVENT_BUS_NAME}...");
      await evbridge.CreateEventBusAsync(new CreateEventBusRequest { Name = EVENT_BUS_NAME });
    }
  }

  private async Task CreateEventBridgeRule(AmazonLambdaClient lambda, AmazonEventBridgeClient evbridge, string funcarn, ObjectChangeTrigger trigger) {
    var rulenm = $"ebr-{funcnm}-{trigger.System}-{trigger.Stage}-{trigger.Object}".ToLower();

    try {
      await evbridge.DescribeRuleAsync(new DescribeRuleRequest { Name = rulenm, EventBusName = EVENT_BUS_NAME });
      Log.Information($"Rule '{rulenm}' already exists on EventBus '{EVENT_BUS_NAME}'");
      return;
    } catch (ResourceNotFoundException) {
      Log.Information($"Rule '{rulenm}' does not exist. Proceeding to create it.");
    }

    var req = new PutRuleRequest {
      Name = rulenm,
      EventPattern = Json.Serialize(new {
        source = new[] { SOURCE_NAME },
        detailType = new[] { nameof(ObjectChangeTrigger) },
        detail = new {
          System = new[] { trigger.System.Value.ToLower() },
          Stage = new[] { trigger.Stage.Value.ToLower() },
          Object = new[] { trigger.Object.Value.ToLower() }
        }
      }).Replace("detailType", "detail-type"),
      // todo GT: use ITemplater, but this class is initiated outside of the DI framework, so consider a better factory method for the Notifiers
      // todo CP: once ITemplater is fixed, CP to implement this using a template
      State = RuleState.ENABLED,
      Description = $"Trigger Lambda on System [{trigger.System}] Stage [{trigger.Stage.Value}] Object [{trigger.Object.Value}]",
      EventBusName = EVENT_BUS_NAME
    };

    var res = await evbridge.PutRuleAsync(req);

    await evbridge.PutTargetsAsync(new PutTargetsRequest {
      Rule = rulenm,
      EventBusName = EVENT_BUS_NAME,
      Targets = [new() { Id = $"ebr-tgt-{rulenm}", Arn = funcarn }]
    });

    try {
      await lambda.AddPermissionAsync(new AddPermissionRequest {
        FunctionName = funcnm,
        StatementId = $"{rulenm}-permission",
        Action = "lambda:InvokeFunction",
        Principal = "events.amazonaws.com",
        SourceArn = res.RuleArn
      });
      Log.Information("Added permission for EventBridge to invoke Lambda function");
    } catch (ResourceConflictException) {
      // Permission already exists - this is fine
      Log.Information("Permission already exists for EventBridge to invoke Lambda function");
    }
  }

}