using Centazio.Core;
using Centazio.Core.Ctl;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Core.Stage;

namespace Centazio.Sample;

public record DummySystemEntyity(Guid Id, string Name);

public class ClickUpReadFunction(IStagedEntityRepository stager, ICtlRepository ctl, ClickUpApi api) :
    ReadFunction(new(nameof(ClickUpReadFunction)), stager, ctl) {

  private readonly string EVERY_X_SECONDS_NCRON = "*/5 * * * * *";

  protected override FunctionConfig<ReadOperationConfig> GetFunctionConfiguration() => new([
    new ReadOperationConfig(new(nameof(DummySystemEntyity)), EVERY_X_SECONDS_NCRON, GetDummySystemEntyityUpdates)
  ]);

  private async Task<ReadOperationResult> GetDummySystemEntyityUpdates(OperationStateAndConfig<ReadOperationConfig> config) => ReadOperationResult.Create(await api.GetTasksAfter(config.Checkpoint));

}

public class ClickUpApi(CentazioSettings settings, CentazioSecrets secrets) {

  private static HttpClient? http; 
  
  public async Task<List<string>> GetTasksAfter(DateTime after) {

    Console.WriteLine($"ClickUpApi TOKEN[{secrets.CLICKUP_TOKEN}] LIST[{settings.ClickUp.ListId}] BASEURL[{settings.ClickUp.BaseUrl}]");
    
    var afterms = new DateTimeOffset(after).ToUnixTimeMilliseconds(); 
    var path = $"list/{settings.ClickUp.ListId}/task?archived=true&page=0&order_by=date_updated&reverse=true&subtasks=true&include_closed=true&date_updated_gt={afterms}";
    using var request = await Client.GetAsync(path);
    var response = await request.Content.ReadAsStringAsync();
    return [response];
  }
  
  private HttpClient Client => http ??= new HttpClient { 
    BaseAddress = new Uri(settings.ClickUp.BaseUrl),
    DefaultRequestHeaders = { {"Authorization", secrets.CLICKUP_TOKEN }, }
  };

}

