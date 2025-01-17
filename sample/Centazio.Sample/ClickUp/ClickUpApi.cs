using Centazio.Core.Misc;

namespace Centazio.Sample.ClickUp;

public class ClickUpApi(SampleSettings settings, SampleSecrets secrets) {

  private static HttpClient? http; 
  
  public async Task<List<TaskJsonAndDateUpdated>> GetTasksAfter(DateTime after) {
    var json = await Query($"list/{settings.ClickUp.ListId}/task?order_by=updated&reverse=true&include_closed=true&date_updated_gt={after.ToMillis()}");
    return Json.SplitList(json, "tasks")
        .Select(taskjson => new TaskJsonAndDateUpdated(taskjson, UtcDate.FromMillis(taskjson, @"""date_updated"":""([^""]+)""")))
        // it is possible for the ClickUp API to include some tasks even though we specify date_updated_gt, so filter manually
        .Where(t => t.LastUpdated > after)
        .OrderBy(t => t.LastUpdated)
        .ToList();
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

public record TaskJsonAndDateUpdated(string Json, DateTime LastUpdated);