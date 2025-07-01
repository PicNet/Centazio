using Centazio.Core.Secrets;

namespace Centazio.Cli.Commands.Dev;

public class LoginToAzCommand(
    ICliSecretsManager loader,
    ICommandRunner cmd) : AbstractCentazioCommand<CommonSettings> {

  public override Task<CommonSettings> GetInteractiveSettings() => Task.FromResult(new CommonSettings());
  
  public override async Task ExecuteImpl(CommonSettings settings) {
    if (!Env.IsInDev) throw new Exception($"{GetType().Name} should not be accessible outside of the Centazio dev environment");
    var secrets = await loader.LoadSecrets<CentazioSecrets>(CentazioConstants.Hosts.Az);
    await cmd.Az($"login --service-principal --username {secrets.AZ_CLIENT_ID} --password {secrets.AZ_SECRET_ID} --tenant {secrets.AZ_TENANT_ID}");
  }
}