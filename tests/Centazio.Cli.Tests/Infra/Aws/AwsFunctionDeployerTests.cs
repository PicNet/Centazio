using Centazio.Cli.Commands.Gen.Cloud;
using Centazio.Cli.Infra.Aws;
using Centazio.Cli.Infra.Dotnet;
using Centazio.Core;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Test.Lib;

namespace Centazio.Cli.Tests.Infra.Aws;

public class AwsFunctionDeployerTests {
  private CentazioSettings settings;
  private CentazioSecrets secrets;
  private ITemplater templater;
  private AwsFunctionProjectMeta project;
  
  [SetUp] public async Task SetUp() {
    settings = await TestingFactories.Settings(CentazioConstants.DEFAULT_ENVIRONMENT, CentazioConstants.Hosts.Aws);
    secrets = await TestingFactories.Secrets();
    templater = new Templater(settings);
    project = await MiscHelpers.AwsEmptyFunctionProject("EmptyFunction");
  }
  
  [Test] public async Task Test_Full_Pipeline_Deployment_to_Aws() {
    var appname = project.AwsFunctionName;
    
    if ((await MiscHelpers.Aws.ListFunctionsInApp(appname)).Count > 0) await MiscHelpers.Aws.DeleteFunctionApp(appname);
    
    var before = await MiscHelpers.Aws.ListFunctionApps();
    
    await new AwsCloudSolutionGenerator(settings, templater, project, ["in-mem"], String.Empty).GenerateSolution();
    await new DotNetCliProjectPublisher(settings, templater).PublishProject(project);
    await new AwsFunctionDeployer(settings, secrets, templater).Deploy(project);
    
    var after = await MiscHelpers.Aws.ListFunctionApps();
    var funcs = await MiscHelpers.Aws.ListFunctionsInApp(appname);
    
    Assert.That(before, Does.Not.Contain(appname));
    Assert.That(after, Does.Contain(appname));
    Assert.That(funcs, Does.Contain(appname));
  }
}
