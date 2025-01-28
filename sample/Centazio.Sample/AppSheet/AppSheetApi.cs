using System.Net.Http.Json;
using System.Text.Json;
using Centazio.Core.Misc;

namespace Centazio.Sample.AppSheet;

// todo: would be good to just accept AppSheetSettings and not whole settings object
public class AppSheetApi(SampleSettings settings, SampleSecrets secrets) {

  private static HttpClient? http;

  private HttpClient Client => http ??= new HttpClient {
    BaseAddress = new Uri(settings.AppSheet.BaseUrl),
    DefaultRequestHeaders = { { "ApplicationAccessKey", secrets.APPSHEET_KEY } }
  };

  public async Task<List<string>> GetAllTasks() => Json.SplitList(await Post(new { Action = "Find" }), String.Empty);

  public async Task<List<AppSheetTask>> AddTasks(List<string> toadd) {
    var res = await Post(new { Action = "Add", Rows = toadd.Select(t => new { Task = t } ) });
    return Json.SplitList<AppSheetTask>(res, "Rows");
  }

  public async Task<List<AppSheetTask>> EditTasks(List<AppSheetTask> toedit) {
    var res = await Post(new { Action = "Edit", Rows = toedit });
    return Json.SplitList<AppSheetTask>(res, "Rows");
  }
  
  public async Task DeleteTasks(List<AppSheetTask> tasks) => await Post(new { Action = "Delete", Rows = tasks.Select(t => new AppSheetTaskId {  RowId = t.RowId }) });

  private async Task<string> Post(object payload) {
    var uri = $"{settings.AppSheet.BaseUrl}/{settings.AppSheet.AppId}/tables/{settings.AppSheet.TableName}/Action";
    var reqjson = JsonContent.Create(payload, options: new JsonSerializerOptions { PropertyNamingPolicy = null });
    var resp = await Client.PostAsync(uri, reqjson);
    return await resp.Content.ReadAsStringAsync();
  }
}