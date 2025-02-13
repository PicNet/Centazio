using Centazio.Cli.Commands.Gen;
using Centazio.Cli.Infra;
using Centazio.Core.Misc;
using Centazio.Test.Lib;

namespace Centazio.Cli.Tests.Infra;

public class FunctionProjectTests {

  [Test] public void Test_propery_values_are_as_expected() {
    var genfolder = TestingFactories.Settings().GeneratedCodeFolder;
    var proj = new FunctionProjectMeta(GetType().Assembly, ECloudEnv.Azure, genfolder);
    
    Assert.That(proj.ProjectName, Is.EqualTo("Centazio.Cli.Tests.Azure"));
    Assert.That(proj.SolutionDirPath, Is.EqualTo(GenRel("Centazio.Cli.Tests.Azure")));
    Assert.That(proj.ProjectDirPath, Is.EqualTo(GenRel("Centazio.Cli.Tests.Azure", "Centazio.Cli.Tests.Azure")));
    // CsprojFile is relative to the solution folder
    Assert.That(proj.CsprojFile, Is.EqualTo(Path.Combine("Centazio.Cli.Tests.Azure", "Centazio.Cli.Tests.Azure.csproj")));
    Assert.That(proj.SlnFilePath, Is.EqualTo(GenRel("Centazio.Cli.Tests.Azure", "Centazio.Cli.Tests.Azure.sln")));
    Assert.That(proj.PublishPath, Is.EqualTo(GenRel("Centazio.Cli.Tests.Azure", "Centazio.Cli.Tests.Azure", "bin", "Release", "net9.0", "publish")));
    

    string GenRel(params string[] steps) => FsUtils.GetSolutionFilePath(steps.Prepend(genfolder).ToArray()); 
  }

}