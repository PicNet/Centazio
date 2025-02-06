using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace Centazio.Cli.Infra;

public static class NugetHelpers {

  public static async Task<List<(string name, string version)>> GetLatestStableVersions(List<string> names) {
    var repo = Repository.Factory.GetCoreV3("https://api.nuget.org/v3/index.json");
    var metadata = await repo.GetResourceAsync<PackageMetadataResource>();
    
    var packages = await Task.WhenAll(names.Select(
        name => metadata.GetMetadataAsync(name, includePrerelease: false, includeUnlisted: false, new SourceCacheContext(), NullLogger.Instance, CancellationToken.None)));
    
    return names.Select((name, idx) => { 
      var version = packages[idx].OrderByDescending(p => p.Identity.Version)
          .FirstOrDefault()
          ?.Identity.Version.ToString() 
              ?? throw new Exception($"could not find nuget package[{name}]");
      return (name, version);
    }).ToList();
  }

}