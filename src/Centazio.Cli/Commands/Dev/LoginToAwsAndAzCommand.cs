using Centazio.Core;
using Centazio.Core.Secrets;
using Centazio.Core.Settings;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Dev;

public class LoginToAwsAndAzCommand(
    [FromKeyedServices(CentazioConstants.Hosts.Az)] CentazioSecrets secrets,
    [FromKeyedServices(CentazioConstants.Hosts.Aws)] AwsSettings coresettings,
    ICommandRunner cmd) : AbstractCentazioCommand<LoginToAwsAndAzCommand.Settings> {

  public override Task<Settings> GetInteractiveSettings() => Task.FromResult(new Settings());
  
  public override async Task ExecuteImpl(Settings settings) {
    if (!Env.IsInDev) throw new Exception($"{GetType().Name} should not be accessible outside of the Centazio dev environment");
    if (!settings.AwsOnly) {
      UiHelpers.Log("Logging into Azure");
      await cmd.Az($"login --service-principal --username {secrets.AZ_CLIENT_ID} --password {secrets.AZ_SECRET_ID} --tenant {secrets.AZ_TENANT_ID}");
    }
    if (!settings.AzOnly) {
      UiHelpers.Log("Logging into AWS");
      await cmd.Aws($"sso login --profile {coresettings.AccountName}");
    }
  }
  public class Settings : CommonSettings {
    [CommandOption("--aws")] public bool AwsOnly { get; init; }
    [CommandOption("--az")] public bool AzOnly { get; init; }
  }
}