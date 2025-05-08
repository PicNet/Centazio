using Amazon;
using Amazon.Lambda;
using Amazon.Runtime;
using Centazio.Cli.Commands.Gen.Cloud;
using Centazio.Cli.Infra.Aws;
using Centazio.Cli.Infra.Dotnet;
using Centazio.Cli.Infra.Misc;
using Centazio.Core;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Centazio.Test.Lib;
using Docker.DotNet;
using Spectre.Console;

namespace Centazio.Cli.Tests.Infra.Aws;

public class AwsFunctionDeployerTests {

  private static readonly CentazioSettings settings = TestingFactories.Settings(CentazioConstants.DEFAULT_ENVIRONMENT, "aws");
  private static readonly CentazioSecrets secrets = TestingFactories.Secrets();
  private readonly ITemplater templater = new Templater(settings);
  private readonly AwsFunctionProjectMeta project = MiscHelpers.AwsEmptyFunctionProject("EmptyFunction");
  
  [Test] public async Task Test_Full_Pipeline_Deployment_to_Aws() {
    // todo: implement aws
     var appname = project.AwsFunctionName;

    RegionEndpoint region = RegionEndpoint.GetBySystemName(settings.AwsSettings.Region);
    using var lambda = new AmazonLambdaClient(new BasicAWSCredentials(secrets.AWS_KEY, secrets.AWS_SECRET), region);

    var before = (await lambda.ListFunctionsAsync()).Functions.Select(f => f.FunctionName);
    if (before.Contains(appname)) await lambda.DeleteFunctionAsync(appname);

    await new AwsCloudSolutionGenerator(settings, templater, project, ["in-mem"]).GenerateSolution();
    await new DotNetCliProjectPublisher(settings, templater).PublishProject(project);
    await new AwsFunctionDeployer(settings, secrets).Deploy(project);

    var after = (await lambda.ListFunctionsAsync()).Functions.Select(f => f.FunctionName);

    Assert.That(before, Does.Not.Contain(appname));
    Assert.That(after, Does.Contain(appname));
  } 
  
  [Test] public async Task Test_aws_zip_file() {
    if (!Directory.Exists(project.PublishPath)) {
      await new AwsCloudSolutionGenerator(settings, templater, project, ["in-mem"]).GenerateSolution();
      await new DotNetCliProjectPublisher(settings, templater).PublishProject(project);
    }
    
    var bytes = await Zip.ZipDir(project.PublishPath, [".exe", ".dll", ".json", ".env", "*.metadata", ".pdb"], []);
    // await File.WriteAllBytesAsync("test_aws_func.zip", bytes);
    Assert.That(bytes, Is.Not.Null);
    Assert.That(bytes, Has.Length.GreaterThan(0));
  }
}
