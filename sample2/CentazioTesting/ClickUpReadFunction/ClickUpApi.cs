using System.Text.Json.Nodes;

namespace CentazioTesting.ClickUp;


public class ClickUpApi(Settings settings, Secrets secrets) {
  
  private static HttpClient? http; 

  private readonly Settings settings = settings ?? throw new ArgumentNullException(nameof(settings));
  private readonly Secrets secrets = secrets ?? throw new ArgumentNullException(nameof(secrets));
  
  public Task<List<object>> GetExampleEntities(DateTime after) {
    return Task.FromResult<List<object>>(null!);
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

public record TaskJsonAndDateUpdated(string Json, DateTime LastUpdated);