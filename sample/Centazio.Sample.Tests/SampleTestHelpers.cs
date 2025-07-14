using Centazio.Sample.Shared;

namespace Centazio.Sample.Tests;

public static class SampleTestHelpers {

  public static async Task<CoreStorageRepository> GetSampleCoreStorage() {
    return await new CoreStorageRepository(() => new CoreStorageDbContext($"Data Source={Guid.NewGuid()};Mode=Memory;Cache=Shared")).Initialise();
  }
  

}