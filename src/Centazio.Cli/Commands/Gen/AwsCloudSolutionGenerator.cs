using Centazio.Cli.Infra;
using Centazio.Core.Settings;
using net.r_eg.MvsSln.Core;

namespace Centazio.Cli.Commands.Gen;

internal class AwsCloudSolutionGenerator(CentazioSettings settings, FunctionProjectMeta project, string environment) : CloudSolutionGenerator(project, environment) {
  
  protected override AbstractCloudProjectGenerator GetCloudProjectGenerator(IXProject proj) => new AwsCloudProjectGenerator(settings, project, proj, environment);

  internal class AwsCloudProjectGenerator(CentazioSettings settings, FunctionProjectMeta projmeta, IXProject slnproj, string environment) : AbstractCloudProjectGenerator(settings, projmeta, slnproj, environment) {

    protected override Task AddCloudSpecificContentToProject(List<Type> functions, Dictionary<string, bool> added) => throw new NotImplementedException();

  }
}