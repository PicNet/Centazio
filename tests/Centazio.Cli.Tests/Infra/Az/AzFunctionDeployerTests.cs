using Centazio.Cli.Commands.Gen;
using Centazio.Cli.Infra;
using Centazio.Cli.Infra.Az;
using Centazio.Test.Lib;

namespace Centazio.Cli.Tests.Infra.Az;

public class AzFunctionDeployerTests {

  [Test] public async Task Test_Deploy() {
    var settings = TestingFactories.Settings();
    var az = new AzFunctionDeployer(settings, TestingFactories.Secrets());
    var project = new GenProject(GetType().Assembly, ECloudEnv.Azure, settings.GeneratedCodeFolder);
    await az.Deploy(project);
  } 

}