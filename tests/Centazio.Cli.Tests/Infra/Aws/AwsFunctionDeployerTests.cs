using Centazio.Cli.Infra.Aws;
using Centazio.Cli.Infra.Dotnet;
using Centazio.Cli.Infra.Misc;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Test.Lib;

namespace Centazio.Cli.Tests.Infra.Aws;

public class AwsFunctionDeployerTests {

  private readonly CentazioSettings settings = TestingFactories.Settings();
  private readonly CentazioSecrets secrets = TestingFactories.Secrets();
  private readonly ITemplater templater = new Templater(TestingFactories.Settings(), TestingFactories.Secrets());
  private readonly FunctionProjectMeta project = MiscHelpers.EmptyFunctionProject(ECloudEnv.Aws, "EmptyFunction");
  
  [Test] public async Task Test_Full_Pipeline_Deployment_to_Aws() {
    var appname = project.DashedProjectName;
    
    // AzCmd.DeleteFunctionApp(appname);
    // var before = AzCmd.ListFunctionApps();
    
    await CloudSolutionGenerator.Create(settings, templater, project, "in-mem").GenerateSolution();
    await new DotNetCliProjectPublisher(settings, templater).PublishProject(project);
    await new AwsFunctionDeployer(settings, secrets).Deploy(project);
    
    // var after = AzCmd.ListFunctionApps();
    // var funcs = AzCmd.ListFunctionsInApp(appname);
    
    // Assert.That(before, Does.Not.Contain(appname));
    // Assert.That(after, Does.Contain(appname));
    // Assert.That(funcs, Does.Contain($"{appname}/EmptyFunction"));
  } 
  
  [Test] public async Task Test_aws_zip_file() {
    if (!Directory.Exists(project.PublishPath)) {
      await CloudSolutionGenerator.Create(settings, templater, project, "in-mem").GenerateSolution();
      await new DotNetCliProjectPublisher(settings, templater).PublishProject(project);
    }
    
    var bytes = await Zip.ZipDir(project.PublishPath, [".exe", ".dll", ".json", ".env", "*.metadata", ".pdb"], []);
    // await File.WriteAllBytesAsync("test_aws_func.zip", bytes);
    Assert.That(bytes, Is.Not.Null);
    Assert.That(bytes, Has.Length.GreaterThan(0));
  }
}
