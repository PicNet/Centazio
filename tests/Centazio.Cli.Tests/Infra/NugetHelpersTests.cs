using System.Xml.Linq;
using Centazio.Cli.Infra;
using Centazio.Core.Misc;

namespace Centazio.Cli.Tests.Infra;

public class NugetHelpersTests {

  [Test] public void Test_PinnedVersions_match_central_package_versions() {
    var props = Path.Combine(FsUtils.FindFileDirectory("Directory.Packages.props") ?? throw new Exception(), "Directory.Packages.props");
    var central = XDocument.Load(props).Descendants("PackageVersion")
        .ToDictionary(e => e.Attribute("Include")?.Value ?? throw new Exception(), e => e.Attribute("Version")?.Value ?? throw new Exception());
    
    Assert.That(central, Is.Not.Empty);
    central.ForEach(kvp => Assert.That(NugetHelpers.PinnedVersions.GetValueOrDefault(kvp.Key), Is.EqualTo(kvp.Value), kvp.Key));
  }
  
  [Test] public async Task Test_GetVersions_uses_pinned_versions_case_insensitively() {
    var versions = await NugetHelpers.GetVersions(["Microsoft.EntityFrameworkCore", "microsoft.azure.functions.worker.sdk"]);
    
    Assert.That(versions, Is.EqualTo(new List<(string, string)> {
      ("Microsoft.EntityFrameworkCore", NugetHelpers.PinnedVersions["Microsoft.EntityFrameworkCore"]),
      ("microsoft.azure.functions.worker.sdk", "2.1.0")
    }));
  }
  
  [Test] public void Test_PatchedTransitives_are_pinned() => 
      NugetHelpers.PatchedTransitives.Values.ForEach(name => Assert.That(NugetHelpers.PinnedVersions.ContainsKey(name), name));
  
  [Test] public async Task Test_GetVersions_adds_patched_transitives() {
    var versions = await NugetHelpers.GetVersions(["Microsoft.Data.Sqlite"]);
    
    Assert.That(versions.Select(v => v.name), Is.EqualTo(new [] { "Microsoft.Data.Sqlite", "SQLitePCLRaw.bundle_e_sqlite3" }));
  }
}
