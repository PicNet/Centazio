using Centazio.Core.Misc;

namespace Centazio.Sample.AppSheet;

// todo: would be good to just accept AppSheetSettings and not whole settings object
// also, I dont like how the CentazioHost registers CentazioSettings, SampleSettings, etc, etc.
public class AppSheetApi(SampleSettings settings, SampleSecrets secrets) {

  private static HttpClient? http;

  private HttpClient Client => http ??= new HttpClient {
    BaseAddress = new Uri(settings.AppSheet.BaseUrl),
    DefaultRequestHeaders = { { "ApplicationAccessKey", secrets.APPSHEET_KEY } }
  };

  public async Task<List<string>> GetAllTasks() => Json.SplitList(await DoPost(new { Action = "Find" }), String.Empty);

  public async Task<List<AppSheetTask>> AddTasks(List<string> toadd) {
    var res = await DoPost(new { Action = "Add", Rows = toadd.Select(t => new { Task = t } ) });
    return Json.SplitList<AppSheetTask>(res, "Rows");
  }

  public async Task<List<AppSheetTask>> EditTasks(List<AppSheetTask> toedit) {
    var res = await DoPost(new { Action = "Edit", Rows = toedit });
    return Json.SplitList<AppSheetTask>(res, "Rows");
  }
  
  public async Task DeleteTasks(List<AppSheetTask> tasks) => await DoPost(new { Action = "Delete", Rows = tasks.Select(t => new AppSheetTaskId {  RowId = t.RowId }) });

  private async Task<string> DoPost(object payload) {
    var uri = $"{settings.AppSheet.BaseUrl}/{settings.AppSheet.AppId}/tables/{settings.AppSheet.TableName}/Action";
    var resp = await Client.PostAsync(uri, Json.SerializeToHttpContent(payload));
    return await resp.Content.ReadAsStringAsync();
  }
}