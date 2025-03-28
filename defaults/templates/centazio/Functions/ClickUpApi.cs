using System.Text.Json.Nodes;

namespace Centazio.Sample.ClickUp;


public class ClickUpApi(Settings settings, Secrets secrets) {

  public static readonly string CLICK_UP_OPEN_STATUS = "to do";
  public static readonly string CLICK_UP_COMPLETE_STATUS = "complete";
  
  private static HttpClient? http; 
  
  public async Task<List<TaskJsonAndDateUpdated>> GetTasksAfter(DateTime after) {
    // https://developer.clickup.com/reference/gettasks
    var json = await Query($"list/{settings.ClickUp.ListId}/task?archived=false&order_by=updated&reverse=true&include_closed=true&date_updated_gt={after.ToMillis()}");
    return Json.SplitList(json, "tasks")
        .Select(taskjson => new TaskJsonAndDateUpdated(taskjson, UtcDate.FromMillis(taskjson, @"""date_updated"":""([^""]+)""")))
        // it is possible for the ClickUp API to include some tasks even though we specify date_updated_gt, so filter manually
        .Where(t => t.LastUpdated > after)
        .OrderBy(t => t.LastUpdated)
        .ToList();
  }
  
  public async Task<string> CreateTask(string name) {
    ArgumentException.ThrowIfNullOrWhiteSpace(name);
    
    var resp = await Client.PostAsync($"list/{settings.ClickUp.ListId}/task", Json.SerializeToHttpContent(new { name }));
    var json = await resp.Content.ReadAsStringAsync();
    var node = JsonNode.Parse(json) ?? throw new Exception();
    return node["id"]?.ToString() ?? throw new Exception();
  }

  public async Task UpdateTask(string id, string name) => await UpdateImpl(id, new { name });
  public async Task OpenTask(string id) => await UpdateImpl(id, new { status = CLICK_UP_OPEN_STATUS });
  public async Task CloseTask(string id) => await UpdateImpl(id, new { status = CLICK_UP_COMPLETE_STATUS });
  public async Task DeleteTask(string id) => await Client.DeleteAsync($"task/{id}");

  // https://developer.clickup.com/reference/updatetask
  private async Task UpdateImpl(string id, object content) => await Client.PutAsync($"task/{id}", Json.SerializeToHttpContent(content));
  

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