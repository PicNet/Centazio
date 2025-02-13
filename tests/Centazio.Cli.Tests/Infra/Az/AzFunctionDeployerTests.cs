using Centazio.Cli.Commands.Gen;
using Centazio.Cli.Infra;
using Centazio.Cli.Infra.Az;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Test.Lib;

namespace Centazio.Cli.Tests.Infra.Az;

public class AzFunctionDeployerTests {

  private readonly CentazioSettings settings = TestingFactories.Settings();
  private readonly CentazioSecrets secrets = TestingFactories.Secrets();
  private readonly FunctionProjectMeta proj = MiscHelpers.EmptyFunctionProject(ECloudEnv.Azure);
  
  [Test] public async Task Test_Deploy() {
    await new ProjectGenerator(proj).GenerateSolution();
    await new AzFunctionDeployer(settings, secrets).Deploy(proj);
  } 

  [Test] public void Test_CreateFunctionAppZip() {
    var path = AzFunctionDeployer.CreateFunctionAppZip(proj);
    
    Assert.That(File.Exists(path));
    File.Delete(path);
  }
}
