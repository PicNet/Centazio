using Centazio.Core.Secrets;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Dev;

public class LoginToAzCommand(
    [FromKeyedServices(CentazioConstants.Hosts.Az)] CentazioSecrets secrets,
    ICommandRunner cmd) : AbstractCentazioCommand<LoginToAzCommand.Settings> {

  public override Task<Settings> GetInteractiveSettings() => Task.FromResult(new Settings());
  
  public override async Task ExecuteImpl(Settings settings) {
    if (!Env.IsInDev) throw new Exception($"{GetType().Name} should not be accessible outside of the Centazio dev environment");
    await cmd.Az($"login --service-principal --username {secrets.AZ_CLIENT_ID} --password {secrets.AZ_SECRET_ID} --tenant {secrets.AZ_TENANT_ID}");
  }
  public class Settings : CommonSettings {
    [CommandOption("--aws")] public bool AwsOnly { get; init; }
    [CommandOption("--az")] public bool AzOnly { get; init; }
  }
}