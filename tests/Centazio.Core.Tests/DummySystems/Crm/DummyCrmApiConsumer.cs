using Centazio.Core.api;

namespace Centazio.Core.Tests.DummySystems.Crm;

public class DummyCrmApiConsumer(string baseurl, HttpClient http) : ICrmApiConsumer {

  public async Task<string> GetCustomers(DateTime sinceutc) {
    var results = await http.GetAsync($"{baseurl}/customers?from={sinceutc:o}");
    return await results.Content.ReadAsStringAsync();
  }
}


public interface ICrmApiConsumer : IApiConsumer { }