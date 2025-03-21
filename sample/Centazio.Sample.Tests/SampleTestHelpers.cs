using Centazio.Providers.Sqlite;
using Centazio.Sample.Shared;

namespace Centazio.Sample.Tests;

public static class SampleTestHelpers {

  public static async Task<CoreStorageRepository> GetSampleCoreStorage(
      [System.Runtime.CompilerServices.CallerFilePath] string path = nameof(SampleTestHelpers), 
      [System.Runtime.CompilerServices.CallerMemberName] string method = nameof(SampleTestHelpers)) {
    var memname = $"{path.Split('\\').Last().Split('.').First()}_{method}".ToLower();
    return await new CoreStorageRepository(() => new CoreStorageDbContext($"Data Source={memname};Mode=Memory;Cache=Shared"), new SqliteDbFieldsHelper()).Initialise();
  }
  

}