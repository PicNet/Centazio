using System.Text.Json;
using Centazio.Core.Tests.DummySystems.Crm;
using Centazio.Test.Lib.Api;
using RichardSzalay.MockHttp;

namespace Centazio.Core.Tests.Api;

public class ApiMockingTests {

  private const string BASE_URL ="https://crm.com/api"; 
  
  [Test] public async Task Test_mocking_crm_api_works_as_expected() {
    var since = UtcDate.UtcNow.AddDays(-1);
    using var mock = new MockApi();
    var cust = new Customer("first", "last", new(2020, 1, 1));
    var response = JsonSerializer.Serialize(new [] { cust });
    mock.Mock.When($"{BASE_URL}/customers")
        .WithQueryString("from", since.ToString("o"))
        .Respond("application/json", response);
    var crm = new DummyCrmApiConsumer(BASE_URL, mock);
    var customers = await crm.GetCustomers(since);
    Assert.That(customers, Is.EqualTo(JsonSerializer.Serialize(new [] { cust })));
  }

}