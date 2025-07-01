using Centazio.Cli.Commands.Gen.Cloud;
using Centazio.Cli.Infra.Aws;
using Centazio.Cli.Infra.Dotnet;
using Centazio.Core;
using Centazio.Core.Misc;
using Centazio.Core.Settings;

namespace Centazio.Cli.Tests.Infra.Aws;

public class AwsFunctionDeployerTests {
  private CentazioSettings settings;
  private ITemplater templater;
  private AwsFunctionProjectMeta project;
  
  [SetUp] public async Task SetUp() {
    settings = await F.Settings(CentazioConstants.DEFAULT_ENVIRONMENT, CentazioConstants.Hosts.Aws);
    templater = new Templater(settings);
    project = await MiscHelpers.AwsEmptyFunctionProject("EmptyFunction");
  }
  
  [Test] public async Task Test_Full_Pipeline_Deployment_to_Aws() {
    // todo CP: test failing on GHA with error: 
    //    WARNING! Your credentials are stored unencrypted in '/root/.docker/config.json'.
    //    Configure a credential helper to remove this warning. See
    //    https://docs.docker.com/go/credential-store/
    if (Env.IsGitHubActions) return; 

    var appname = project.AwsFunctionName;
    
    if ((await MiscHelpers.Aws.ListFunctionsInApp(appname)).Count > 0) await MiscHelpers.Aws.DeleteFunctionApp(appname);
    
    var before = await MiscHelpers.Aws.ListFunctionApps();
    
    await new AwsCloudSolutionGenerator(settings, templater, project, ["in-mem"], String.Empty).GenerateSolution();
    await new DotNetCliProjectPublisher(settings, templater).PublishProject(project);
    await new AwsFunctionDeployer(settings, TestingCliSecretsManager.Instance, templater).Deploy(project);
    
    var after = await MiscHelpers.Aws.ListFunctionApps();
    var funcs = await MiscHelpers.Aws.ListFunctionsInApp(appname);
    
    Assert.That(before, Does.Not.Contain(appname));
    Assert.That(after, Does.Contain(appname));
    Assert.That(funcs, Does.Contain(appname));
  }
}
