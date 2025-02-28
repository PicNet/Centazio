using Centazio.Cli.Infra.Dotnet;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;

namespace Centazio.Cli.Infra.Az;

public interface IAzFunctionDeleter {
  Task Delete(FunctionProjectMeta project);
}

// try to replicate command: az functionapp deployment source config-zip -g <resource group name> -n <function app name> --src <zip file path>
public class AzFunctionDeleter(CentazioSettings settings, CentazioSecrets secrets, ITemplater templater, ICommandRunner cmd) : AbstractAzCommunicator(secrets), IAzFunctionDeleter {

  
  public async Task Delete(FunctionProjectMeta project) {
    await Task.Run(() => cmd.Az(templater.ParseFromContent(settings.Defaults.ConsoleCommands.Az.DeleteFunctionApp, new { AppName = project.DashedProjectName }), quiet: true));
  }
}

