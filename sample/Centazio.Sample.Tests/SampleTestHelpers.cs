using Centazio.Providers.Sqlite;

namespace Centazio.Sample.Tests;

public static class SampleTestHelpers {

  public static async Task<SampleCoreStorageRepository> GetSampleCoreStorage(
      [System.Runtime.CompilerServices.CallerFilePath] string path = nameof(SampleTestHelpers), 
      [System.Runtime.CompilerServices.CallerMemberName] string method = nameof(SampleTestHelpers)) {
    var file = $"{path.Split('\\').Last().Split('.').First()}_{method}.db".ToLower();
    File.Delete(file);
    return await new SampleCoreStorageRepository(() => new SampleDbContext($"Data Source={file}"), new SqliteDbFieldsHelper()).Initialise();
  }
  

}