using Centazio.Cli.Commands.Gen;
using Centazio.Core;
using Centazio.Core.Ctl;
using Centazio.Core.Misc;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Core.Settings;
using Centazio.Core.Stage;
using Centazio.Test.Lib;

namespace Centazio.Cli.Tests.Infra.Gen;

public class ProjectGeneratorTests {

  [Test] public async Task Test_GenerateSolution() {
    var settings = TestingFactories.Settings<CentazioSettings>();
    var proj = $"{GetType().Assembly.GetName().Name}.{ECloudEnv.Azure}";
    var expdir = FsUtils.GetSolutionFilePath(settings.GeneratedCodeFolder, proj);
    // Directory.Delete(expdir, true); // does not work as generated directory is locked?
    
    await new ProjectGenerator(settings.GeneratedCodeFolder, ECloudEnv.Azure, GetType().Assembly).GenerateSolution();
    Assert.That(Directory.Exists(expdir));
  }

}

public class TestReadFunction(IEntityStager stager, ICtlRepository ctl) : ReadFunction(new(nameof(ProjectGeneratorTests)), stager, ctl) {

  protected override FunctionConfig<ReadOperationConfig> GetFunctionConfiguration() => throw new Exception();

}