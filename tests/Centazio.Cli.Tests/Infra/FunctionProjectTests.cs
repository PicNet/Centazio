using Centazio.Core;
using Centazio.Core.Misc;
using Centazio.Test.Lib;

namespace Centazio.Cli.Tests.Infra;

public class FunctionProjectTests {

  [Test] public async Task Test_propery_values_are_as_expected() {
    var settings = await TestingFactories.Settings(CentazioConstants.DEFAULT_ENVIRONMENT, CentazioConstants.Hosts.Az);
    var proj = new AzFunctionProjectMeta(GetType().Assembly, settings, new Templater(settings));

    Assert.That(proj.ProjectName, Is.EqualTo("Centazio.Cli.Tests.Az"));
    Assert.That(proj.SolutionDirPath, Is.EqualTo(GenRel("Centazio.Cli.Tests.Az")));
    Assert.That(proj.ProjectDirPath, Is.EqualTo(GenRel("Centazio.Cli.Tests.Az")));
    Assert.That(proj.CsprojFile, Is.EqualTo("Centazio.Cli.Tests.Az.csproj"));
    Assert.That(proj.SlnFilePath, Is.EqualTo(GenRel("Centazio.Cli.Tests.Az", "Centazio.Cli.Tests.Az.sln")));
    Assert.That(proj.PublishPath, Is.EqualTo(GenRel("Centazio.Cli.Tests.Az", "bin", "Release", "net9.0", "publish")));
    
    string GenRel(params List<string> steps) => FsUtils.GetCentazioPath(steps.Prepend(settings.Defaults.GeneratedCodeFolder).ToList()); 
  }

}