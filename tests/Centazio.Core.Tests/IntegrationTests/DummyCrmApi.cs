using System.Text.Json;
using Centazio.Test.Lib;

namespace Centazio.Core.Tests.IntegrationTests;

internal class DummyCrmApi {

  // this list mantains a list of customers in the dummy database.   A new customer is added
  //    each second (utc.Now - TEST_START_DT) with the LastUpdate date being utc.Now 
  private readonly List<CrmCustomer> customers = new();

  internal Task<List<string>> GetCustomersUpdatedSince(DateTime after) {
    UpdateCustomerList();
    return Task.FromResult(customers.Where(c => c.LastUpdate > after).Select(c => JsonSerializer.Serialize(c)).ToList());
  }

  private void UpdateCustomerList() {
    var now = UtcDate.UtcNow;
    var expcount = (int)(now - TestingDefaults.DefaultStartDt).TotalSeconds;
    var start = customers.Count;
    var missing = expcount - start;
    Enumerable.Range(0, missing)
        .ForEachIdx(missidx => {
          var idx = start + missidx;
          customers.Add(NewCust(idx));
        });
  }

  internal static CrmCustomer NewCust(int idx, DateTime? updated = null) => new(
      Guid.Parse($"00000000-0000-0000-0000-{idx.ToString().PadLeft(12, '0')}"), 
      idx.ToString(), 
      idx.ToString(), 
      DateOnly.FromDateTime(TestingDefaults.DefaultStartDt.AddYears(-idx)), 
      updated ?? UtcDate.UtcNow);
}