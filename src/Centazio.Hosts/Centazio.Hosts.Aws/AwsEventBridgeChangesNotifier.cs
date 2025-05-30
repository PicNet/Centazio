using Amazon.EventBridge;
using Amazon.EventBridge.Model;
using Centazio.Core;
using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Serilog;

namespace Centazio.Hosts.Aws;

public class AwsEventBridgeChangesNotifier : IChangesNotifier, IDisposable {

  public const string SOURCE_NAME = "centazio";
  public const string EVENT_BUS_NAME = "centazio-event-bus";

  public void Dispose() { }

  public void Init(List<IRunnableFunction> functions) { }

  public Task Run(IFunctionRunner runner) => throw new NotImplementedException();

  public async Task Notify(SystemName system, LifecycleStage stage, List<ObjectName> objs) {
    var client = new AmazonEventBridgeClient();
    var putEventsRequest = new PutEventsRequest {
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

    var response = await client.PutEventsAsync(putEventsRequest);
    response.Entries.ForEach(result => {
      Log.Information(!string.IsNullOrEmpty(result.EventId) ? $"Event published successfully. Event ID: {result.EventId}" : $"Failed to publish event. Error: {result.ErrorMessage}");
    });
  }

  public bool IsAsync => true;

}