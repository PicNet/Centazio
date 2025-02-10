using Centazio.Cli.Commands.Gen;
using Centazio.Cli.Infra;
using Centazio.Cli.Infra.Az;
using Centazio.Cli.Infra.Dotnet;
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
  
  // try to replicate command: az functionapp deployment source config-zip -g <resource group name> -n <function app name> --src <zip file path>
  [Test] public Task Test_az_deploy_manual_command() {
    // await new DotNetCliProjectPublisher().BuildProject(proj);
    
    var path = AzFunctionDeployer.CreateFunctionAppZip(proj);
    Console.WriteLine(path);
    // var path = @"C:\dev\projects\internal_projects\manual_azure_func\bin\Release\net9.0\publish\manual_azure_func.zip";
    // var (rg, fnapp) = (settings.AzureSettings.ResourceGroup, "test-centazio-3");
    // new CommandRunner().Az($"functionapp deployment source config-zip -g {rg} -n {fnapp} --src \"{path}\"");
    return Task.CompletedTask;
  }
}
