using Centazio.Cli.Infra.Az;
using Centazio.Cli.Infra.Dotnet;
using Centazio.Cli.Infra.Misc;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Test.Lib;
using AzCmd = Centazio.Cli.Tests.MiscHelpers.Az;

namespace Centazio.Cli.Tests.Infra.Az;

public class AzFunctionDeployerTests {

  private readonly CentazioSettings settings = TestingFactories.Settings();
  private readonly CentazioSecrets secrets = TestingFactories.Secrets();
  private readonly ITemplater templater = new Templater(TestingFactories.Settings(), TestingFactories.Secrets());
  private readonly AzureFunctionProjectMeta project = MiscHelpers.AzureEmptyFunctionProject();
  
  [Test, Ignore("slow")] public async Task Test_Full_Pipeline_Deployment_to_Azure() {
    var appname = project.DashedProjectName;
    
    AzCmd.DeleteFunctionApp(appname);
    var before = AzCmd.ListFunctionApps();
    
    await new AzureCloudSolutionGenerator(settings, templater, project, "in-mem").GenerateSolution();
    await new DotNetCliProjectPublisher(settings, templater).PublishProject(project);
    await new AzFunctionDeployer(settings, secrets).Deploy(project);
    
    var after = AzCmd.ListFunctionApps();
    var funcs = AzCmd.ListFunctionsInApp(appname);
    
    Assert.That(before, Does.Not.Contain(appname));
    Assert.That(after, Does.Contain(appname));
    Assert.That(funcs, Does.Contain($"{appname}/EmptyFunction"));
  } 
  
  [Test] public async Task Test_CreateFunctionAppZip() {
    if (!Directory.Exists(project.PublishPath)) {
      await new AzureCloudSolutionGenerator(settings, templater, project, "in-mem").GenerateSolution();
      await new DotNetCliProjectPublisher(settings, templater).PublishProject(project);
    }
    
    var bytes = await Zip.ZipDir(project.PublishPath, [".exe", ".dll", ".json", ".env", "*.metadata", ".pdb"], [".azurefunctions", "runtimes"]);
    // await File.WriteAllBytesAsync("test_az_func.zip", bytes);
    Assert.That(bytes, Is.Not.Null);
    Assert.That(bytes, Has.Length.GreaterThan(0));
  }
}
