using Centazio.Cli.Infra.CodeGen.Csproj;
using Centazio.Core;
using Centazio.Core.Ctl;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Core.Settings;
using Centazio.Core.Stage;
using Centazio.Test.Lib;

namespace Centazio.Cli.Tests.Infra.CodeGen.Csproj;

public class ProjectGeneratorTests {

  [Test] public async Task Test_GenerateSolution() {
    var settings = TestingFactories.Settings<CentazioSettings>();
    await new ProjectGenerator(settings.GeneratedCodeFolder, ECloudEnv.Azure, typeof(TestReadFunction).Assembly).GenerateSolution();
  }

}

public class TestReadFunction(IEntityStager stager, ICtlRepository ctl) : ReadFunction(new(nameof(ProjectGeneratorTests)), stager, ctl) {

  protected override FunctionConfig<ReadOperationConfig> GetFunctionConfiguration() => throw new NotImplementedException();

}