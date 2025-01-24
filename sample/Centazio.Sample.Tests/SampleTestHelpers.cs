using Centazio.Providers.Sqlite;

namespace Centazio.Sample.Tests;

public static class SampleTestHelpers {

  public static async Task<SampleCoreStorageRepository> GetSampleCoreStorage([System.Runtime.CompilerServices.CallerFilePath] string path = nameof(SampleTestHelpers)) {
    var file = $"{path.Split('\\').Last()}.db";
    File.Delete(file);
    var core = new SampleCoreStorageRepository(() => new SampleDbContext($"Data Source={file}"), new SqliteDbFieldsHelper());
    await core.Initialise();
    return core;
  }
  

}