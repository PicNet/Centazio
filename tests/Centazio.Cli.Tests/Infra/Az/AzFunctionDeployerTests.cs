using Centazio.Cli.Infra.Az;
using Centazio.Test.Lib;

namespace Centazio.Cli.Tests.Infra.Az;

public class AzFunctionDeployerTests {

  [Test] public async Task Test_Deploy() {
    var az = new AzFunctionDeployer(TestingFactories.Settings(), TestingFactories.Secrets());
    await az.Deploy(GetType().Assembly.GetName().Name +  ".Azure");
  } 

}