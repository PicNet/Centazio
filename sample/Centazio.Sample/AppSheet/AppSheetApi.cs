using Centazio.Core.Misc;

namespace Centazio.Sample.AppSheet;

public class AppSheetApi(AppSheetSettings settings, SampleSecrets secrets) {

  private static HttpClient? http;

  private HttpClient Client => http ??= new HttpClient {
    BaseAddress = new Uri(settings.BaseUrl),
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
    var uri = $"{settings.BaseUrl}/{settings.AppId}/tables/{settings.TableName}/Action";
    var resp = await Client.PostAsync(uri, Json.SerializeToHttpContent(payload));
    return await resp.Content.ReadAsStringAsync();
  }
}