using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Centazio.Core.Stage;

namespace Centazio.Sample.ClickUp;


public class ClickUpApi(Settings settings, Secrets secrets) {

  public static readonly string CLICK_UP_OPEN_STATUS = "to do";
  public static readonly string CLICK_UP_COMPLETE_STATUS = "complete";
  
  private static HttpClient? http; 
  
  public async Task<List<RawJsonData>> GetTasksAfter(DateTime after) {
    // https://developer.clickup.com/reference/gettasks
    var json = await Query($"list/{settings.CustomSetting.ListId}/task?archived=false&order_by=updated&reverse=true&include_closed=true&date_updated_gt={after.ToMillis()}");
    return Json.SplitList(json, "tasks")
        .Select(taskjson => {
          var id = Regex.Match(taskjson, @"""id"":""([^""]+)""").Groups[1].Value;
          var updatedutc = UtcDate.FromMillis(taskjson, @"""date_updated"":""([^""]+)""");
          return new RawJsonData(taskjson, id, updatedutc);
        })
        // it is possible for the ClickUp API to include some tasks even though we specify date_updated_gt, so filter manually
        .Where(t => t.LastUpdatedUtc > after)
        .OrderBy(t => t.LastUpdatedUtc)
        .ToList();
  }
  
  public async Task<string> CreateTask(string name) {
    ArgumentException.ThrowIfNullOrWhiteSpace(name);
    
    var resp = await Client.PostAsync($"list/{settings.CustomSetting.ListId}/task", Json.SerializeToHttpContent(new { name }));
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
    BaseAddress = new Uri(settings.CustomSetting.BaseUrl),
    DefaultRequestHeaders = { {"Authorization", secrets.CLICKUP_TOKEN }, }
  };

}
