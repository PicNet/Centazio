using Microsoft.Extensions.DependencyInjection;

namespace Centazio.Cli.Commands.Dev;

public class LoginToAwsCommand(
    [FromKeyedServices(CentazioConstants.Hosts.Aws)] AwsSettings coresettings,
    ICommandRunner cmd) : AbstractCentazioCommand<CommonSettings> {

  public override Task<CommonSettings> GetInteractiveSettings() => Task.FromResult(new CommonSettings());
  
  public override async Task ExecuteImpl(CommonSettings settings) {
    if (!Env.IsInDev) throw new Exception($"{GetType().Name} should not be accessible outside of the Centazio dev environment");

    try { await cmd.Aws($"sso login --profile {coresettings.AccountName}"); }
    catch (Exception e) {
      if (e.Message.Contains($"The config profile ({coresettings.AccountName}) could not be found")) throw new Exception($"Please run 'aws configure sso --profile {coresettings.AccountName}' first");
      throw;
    }
  }
}