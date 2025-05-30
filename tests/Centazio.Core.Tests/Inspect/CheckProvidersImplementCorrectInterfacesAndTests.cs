using Centazio.Test.Lib.BaseProviderTests;

namespace Centazio.Core.Tests.Inspect;

public class CheckProvidersImplementCorrectInterfacesAndTests {
  
  private readonly List<string> IGNORE = [
    nameof(BaseSimulationCoreStorageRepositoryTests),
    nameof(BaseStagedEntityRepositoryTests),
    nameof(BaseSecretsLoaderTests)];
  
  [Test] public void Test_all_providers_implement_base_provider_tests() {
    var basetestsdir = FsUtils.GetCentazioPath("src", "Centazio.Test.Lib", "BaseProviderTests");
    var tests = InspectUtils.CsFiles(basetestsdir)
        .Select(f => f.Split(Path.DirectorySeparatorChar).Last().Split('.').First())
        .Where(n => !IGNORE.Contains(n)).ToList();
    var provsdir = FsUtils.GetCentazioPath("src", "Centazio.Providers");
    var provs = Directory.GetDirectories(provsdir).Where(dir => !dir.EndsWith("Centazio.Providers.EF")).ToList();
    var testsdir = FsUtils.GetCentazioPath("tests");
    var provtests = Directory.GetDirectories(testsdir).Where(d => d.IndexOf($"{Path.DirectorySeparatorChar}Centazio.Providers.", StringComparison.OrdinalIgnoreCase) >= 0).ToList();
    
    var errors = new List<string>();
    provs.ForEach(provdir => {
      var provname = provdir.Split(Path.DirectorySeparatorChar).Last();
      var provtestdir = provtests.SingleOrDefault(testdir => testdir.EndsWith($"{provname}.Tests"));
      if (provtestdir is null) {
        errors.Add($"Provider[{provname}] does not have a matching unit test project");
        return;
      }
      var provider = provname.Split('.').Last();
      var provtestfiles = InspectUtils.CsFiles(provtestdir).Select(f => f.Split(Path.DirectorySeparatorChar).Last().Split('.').First()).ToList();
      tests.ForEach(test => {
        if (IsStageEntityOnly(provider) && test != nameof(BaseStagedEntityRepositoryTests)) return;
        var exp = test.Replace("Base", provider);
        if (provtestfiles.FindIndex(ptf => ptf.EndsWith(exp)) < 0) errors.Add($"Provider[{provname}] does not have test[{exp}]");
      });
    });
    
    Assert.That(errors, Is.Empty, String.Join('\n', errors));
    
    bool IsStageEntityOnly(string prov) => prov is "Aws" or "Az" ;
  }
}