using Centazio.Core.Stage;
using Centazio.Test.Lib;

namespace Centazio.Core.Tests.IntegrationTests;

internal class DummyCrmApi {

  // this list mantains a list of customers in the dummy database.   A new customer is added
  //    each second (utc.Now - TEST_START_DT) with the LastUpdate date being utc.Now 
  private readonly List<System1Entity> customers = [];

  internal Task<List<RawJsonData>> GetCustomersUpdatedSince(DateTime after) {
    UpdateCustomerList();
    var data = customers
        .Where(c => c.LastUpdatedDate > after)
        .Select(c => new RawJsonData(Json.Serialize(c), c.SystemId, c.LastUpdatedDate))
        .ToList();
    return Task.FromResult(data);
  }

  private void UpdateCustomerList() {
    var now = UtcDate.UtcNow;
    var expcount = (int)(now - TestingDefaults.DefaultStartDt).TotalSeconds;
    var start = customers.Count;
    var missing = expcount - start;
    Enumerable.Range(0, missing)
        .ForEach(missidx => {
          var idx = start + missidx;
          customers.Add(NewCust(idx));
        });
  }

  internal static System1Entity NewCust(int idx, DateTime? updated = null) {
    var sysid = Guid.Parse($"00000000-0000-0000-0000-{idx.ToString().PadLeft(12, '0')}");
    return new (sysid,
        CorrelationId.Build(C.System1Name, C.SystemEntityName, new(sysid.ToString())), 
        idx.ToString(),
        idx.ToString(),
        DateOnly.FromDateTime(TestingDefaults.DefaultStartDt.AddYears(-idx)),
        updated ?? UtcDate.UtcNow);
  }

}