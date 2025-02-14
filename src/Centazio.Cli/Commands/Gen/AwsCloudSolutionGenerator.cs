using Centazio.Cli.Infra;
using net.r_eg.MvsSln.Core;

namespace Centazio.Cli.Commands.Gen;

internal class AwsCloudSolutionGenerator(FunctionProjectMeta project, string environment) : CloudSolutionGenerator(project, environment) {
  
  protected override AbstractCloudProjectGenerator GetCloudProjectGenerator(IXProject proj) => new AwsCloudProjectGenerator(project, proj, environment);

  internal class AwsCloudProjectGenerator(FunctionProjectMeta projmeta, IXProject slnproj, string environment) : AbstractCloudProjectGenerator(projmeta, slnproj, environment) {

    protected override Task AddCloudSpecificContentToProject(List<Type> functions) => throw new NotImplementedException();

  }
}