using Centazio.Cli.Commands.Gen;
using Centazio.Cli.Infra;
using Centazio.Cli.Infra.Az;
using Centazio.Cli.Infra.Dotnet;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Test.Lib;
using AzCmd = Centazio.Cli.Tests.MiscHelpers.Az;

namespace Centazio.Cli.Tests.Infra.Az;

public class AzFunctionDeployerTests {

  private readonly CentazioSettings settings = TestingFactories.Settings();
  private readonly CentazioSecrets secrets = TestingFactories.Secrets();
  private readonly ITemplater templater = new Templater(TestingFactories.Settings(), TestingFactories.Secrets());
  private readonly FunctionProjectMeta project = MiscHelpers.EmptyFunctionProject(ECloudEnv.Azure);
  
  [Test, Ignore("slow")] public async Task Test_Full_Pipeline_Deployment_to_Azure() {
    var appname = project.DashedProjectName;
    
    AzCmd.DeleteFunctionApp(appname);
    var before = AzCmd.ListFunctionApps();
    
    await CloudSolutionGenerator.Create(settings, templater, project, "azure").GenerateSolution();
    await new DotNetCliProjectPublisher(settings, templater).PublishProject(project);
    await new AzFunctionDeployer(settings, secrets).Deploy(project);
    
    var after = AzCmd.ListFunctionApps();
    var funcs = AzCmd.ListFunctionsInApp(appname);
    
    Assert.That(before, Does.Not.Contain(appname));
    Assert.That(after, Does.Contain(appname));
    Assert.That(funcs, Does.Contain($"{appname}/EmptyFunction"));
  } 
  
  [Test] public async Task Test_CreateFunctionAppZip() {
    await CloudSolutionGenerator.Create(settings, templater, project, "azure_sample").GenerateSolution();
    await new DotNetCliProjectPublisher(settings, templater).PublishProject(project);
    
    var path = AzFunctionDeployer.CreateFunctionAppZip(project);
    
    Assert.That(File.Exists(path));
    File.Delete(path);
  }
}
