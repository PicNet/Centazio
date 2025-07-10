using Centazio.Core.Stage;
using System.Text.Json.Nodes;

namespace {{ it.Namespace }};


public class {{ it.SystemName }}Api(Settings settings, Secrets secrets) {
  
  private static HttpClient? http; 

  private readonly Settings settings = settings ?? throw new ArgumentNullException(nameof(settings));
  private readonly Secrets secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
  
  public Task<List<RawJsonData>> GetExampleEntities(DateTime after) {
    return Task.FromResult<List<RawJsonData>>(null!);
  }

  public Task<string> CreateExampleEntity(string name) {
    return Task.FromResult(String.Empty);
  }

  public Task UpdateExampleEntity(string id, string name) {
    return Task.CompletedTask;
  }
  
  private HttpClient Client => http ??= new HttpClient { 
    BaseAddress = new Uri("target system api url"),
    DefaultRequestHeaders = { {"Authorization", "get auth header from secrets" }, }
  };

}
