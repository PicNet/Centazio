using Centazio.Cli.Commands.Gen.Cloud;
using Centazio.Cli.Infra.Aws;
using Centazio.Cli.Infra.Dotnet;
using Centazio.Cli.Infra.Misc;
using Centazio.Core;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Test.Lib;

namespace Centazio.Cli.Tests.Infra.Aws;

public class AwsFunctionDeployerTests {

  private static readonly CentazioSettings settings = TestingFactories.Settings(CentazioConstants.DEFAULT_ENVIRONMENT, "aws");
  private static readonly CentazioSecrets secrets = TestingFactories.Secrets();
  private readonly ITemplater templater = new Templater(settings);
  private readonly AwsFunctionProjectMeta project = MiscHelpers.AwsEmptyFunctionProject("EmptyFunction");
  
  [Test] public async Task Test_Full_Pipeline_Deployment_to_Aws() {
    var appname = project.AwsFunctionName;
    
    if ((await MiscHelpers.Aws.ListFunctionsInApp(appname)).Count > 0) await MiscHelpers.Aws.DeleteFunctionApp(appname);
    
    var before = await MiscHelpers.Aws.ListFunctionApps();
    
    await new AwsCloudSolutionGenerator(settings, templater, project, ["in-mem"]).GenerateSolution();
    await new DotNetCliProjectPublisher(settings, templater).PublishProject(project);
    await new AwsFunctionDeployer(settings, secrets).Deploy(project);
    
    var after = await MiscHelpers.Aws.ListFunctionApps();
    var funcs = await MiscHelpers.Aws.ListFunctionsInApp(appname);
    
    Assert.That(before, Does.Not.Contain(appname));
    Assert.That(after, Does.Contain(appname));
    Assert.That(funcs, Does.Contain(appname));
  }
}
