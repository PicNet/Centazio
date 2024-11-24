using Centazio.Core;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;

namespace Centazio.Sample;

public class ClickUpApi(CentazioSettings settings, CentazioSecrets secrets) {

  private static HttpClient? http; 
  
  public async Task<string> GetTasksAfter(DateTime after) {
    var path = $"list/{settings.ClickUp.ListId}/task?order_by=updated&reverse=true&include_closed=true&date_updated_gt={after.ToMillis()}";
    using var request = await Client.GetAsync(path);
    return await request.Content.ReadAsStringAsync();
  }
  
  private HttpClient Client => http ??= new HttpClient { 
    BaseAddress = new Uri(settings.ClickUp.BaseUrl),
    DefaultRequestHeaders = { {"Authorization", secrets.CLICKUP_TOKEN }, }
  };

}