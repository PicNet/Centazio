using System.Reflection;
using NuGet.Common;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;

namespace Centazio.Cli.Infra;

public static class NugetHelpers {

  private const string PREFIX = "package:";
  
  internal static readonly Dictionary<string, string> PinnedVersions = LoadPinnedVersions();
  
  // generated projects do not use central package management, so the transitive packages Directory.Packages.props 
  //    raises to patched versions are referenced directly, keyed by the package that brings them in
  internal static readonly Dictionary<string, string> PatchedTransitives = new(StringComparer.OrdinalIgnoreCase) {
    ["Microsoft.Data.Sqlite"] = "SQLitePCLRaw.bundle_e_sqlite3"
  };

  /// <summary>
  /// Versions for the packages of a generated project: the version Centazio was built and tested with, or the
  /// latest stable version for a package Centazio does not pin.  Adds the patched versions of vulnerable transitive 
  /// packages.
  /// </summary>
  public static async Task<List<(string name, string version)>> GetVersions(List<string> packages) {
    var names = packages.Concat(packages.Select(PatchedTransitives.GetValueOrDefault).OfType<string>()).Distinct().ToList();
    var unpinned = names.Where(name => !PinnedVersions.ContainsKey(name)).ToList();
    if (unpinned.Any()) Log.Warning("packages not pinned, using latest stable {@Packages}", unpinned);
    
    var latest = (await GetLatestStableVersions(unpinned)).ToDictionary(p => p.name, p => p.version);
    return names.Select(name => (name, PinnedVersions.GetValueOrDefault(name) ?? latest[name])).ToList();
  }

  public static async Task<List<(string name, string version)>> GetLatestStableVersions(List<string> names) {
    if (!names.Any()) return [];
    
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
  
  private static Dictionary<string, string> LoadPinnedVersions() => 
      typeof(NugetHelpers).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
          .Where(a => a.Key.StartsWith(PREFIX))
          .ToDictionary(a => a.Key[PREFIX.Length..], a => a.Value ?? throw new Exception($"no version [{a.Key}]"), StringComparer.OrdinalIgnoreCase);

}
