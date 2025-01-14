using Centazio.Core;

namespace Centazio.Sample;

public class ClickUpApi(SampleSettings settings, SampleSecrets secrets) {

  private static HttpClient? http; 
  
  public async Task<List<string>> GetTasksAfter(DateTime after) {
    var json = await Query($"list/{settings.ClickUp.ListId}/task?order_by=updated&reverse=true&include_closed=true&date_updated_gt={after.ToMillis()}");
    var tasks = Json.SplitList(json, "tasks");
    return tasks;
  }

  private async Task<string> Query(string path) {
    using var request = await Client.GetAsync(path);
    return await request.Content.ReadAsStringAsync();
  }

  private HttpClient Client => http ??= new HttpClient { 
    BaseAddress = new Uri(settings.ClickUp.BaseUrl),
    DefaultRequestHeaders = { {"Authorization", secrets.CLICKUP_TOKEN }, }
  };

}