using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Centazio.Core.Misc;

namespace Centazio.Sample.AppSheet;

// todo: would be good to just accept AppSheetSettings and not whole settings object
public class AppSheetApi(SampleSettings settings, SampleSecrets secrets) {

  private static HttpClient? http;

  private HttpClient Client => http ??= new HttpClient(new LoggingHandler(new HttpClientHandler())) {
    BaseAddress = new Uri(settings.AppSheet.BaseUrl),
    DefaultRequestHeaders = { { "ApplicationAccessKey", secrets.APPSHEET_KEY } }
  };

  public async Task<List<string>> GetAllTasks() {
    var uri = $"{settings.AppSheet.BaseUrl}/{settings.AppSheet.AppId}/tables/{settings.AppSheet.TableName}/Action";
    var reqjson = JsonContent.Create(new { Action = "Find" }, options: new JsonSerializerOptions { PropertyNamingPolicy = null });
    var resp = await Client.PostAsync(uri, reqjson);
    var json = await resp.Content.ReadAsStringAsync();
    return Json.SplitList(json, String.Empty);
  }

}

public record AppSheetTask {
  [JsonPropertyName("Row ID")] public string? RowId { get; set; }
  public string? Task { get; set; }
}

public class LoggingHandler(HttpMessageHandler innerHandler) : DelegatingHandler(innerHandler) {

  protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
    Console.WriteLine("Request:");
    Console.WriteLine(request.ToString());
    if (request.Content != null) Console.WriteLine(await request.Content.ReadAsStringAsync());
    Console.WriteLine();

    var response = await base.SendAsync(request, cancellationToken);

    Console.WriteLine("Response:");
    Console.WriteLine(response.ToString());
    Console.WriteLine(await response.Content.ReadAsStringAsync());
    Console.WriteLine();

    return response;
  }

}