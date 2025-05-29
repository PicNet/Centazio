using Centazio.Core;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace Centazio.Cli.Infra.Az;

public interface IAzFunctionDeleter {
  Task Delete(AzFunctionProjectMeta project);
}

// try to replicate command: az functionapp deployment source config-zip -g <resource group name> -n <function app name> --src <zip file path>
public class AzFunctionDeleter([FromKeyedServices(CentazioConstants.Hosts.Az)] CentazioSettings settings, [FromKeyedServices(CentazioConstants.Hosts.Az)] CentazioSecrets secrets, ITemplater templater, ICommandRunner cmd) : AbstractAzCommunicator(secrets), IAzFunctionDeleter {

  
  public async Task Delete(AzFunctionProjectMeta project) {
    await Task.Run(() => cmd.Az(templater.ParseFromContent(settings.Defaults.ConsoleCommands.Az.DeleteFunctionApp, new { AppName = project.DashedProjectName }), quiet: true));
  }
}

