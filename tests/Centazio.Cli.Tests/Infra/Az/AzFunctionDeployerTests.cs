using Centazio.Cli.Commands.Az;
using Centazio.Test.Lib;

namespace Centazio.Cli.Tests.Infra.Az;

public class AzFunctionDeployerTests {

  [Test] public async Task Test_Deploy() {
    var az = new AzFunctionDeployer(TestingFactories.Settings(), TestingFactories.Secrets());
    await az.Deploy("testname", @"C:\dev\projects\internal_projects\centazio3\generated\Centazio.Core.Tests.Azure\Centazio.Core.Tests.Azure.sln");
  } 

}