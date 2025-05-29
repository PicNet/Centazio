using Centazio.Cli.Infra.Aws;
using Centazio.Core.Settings;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Aws;

public class AddAccountCommand([FromKeyedServices("aws")] CentazioSettings clisetts, IAwsAccounts impl) 
    : AbstractCentazioCommand<AddAccountCommand.Settings> {
  
  public override Task<Settings> GetInteractiveSettings() => Task.FromResult(new Settings {
    AccountName = UiHelpers.Ask("Account Name", clisetts.AwsSettings.AccountName) 
  });

  public override async Task ExecuteImpl(Settings settings) {
    if (String.IsNullOrWhiteSpace(settings.AccountName)) throw new Exception(Interactive ? "AccountName is required" : "<ACCOUNT_NAME> is required");
    await UiHelpers.ProgressWithErrorMessage("Creating account", async () => await impl.AddAccount(settings.AccountName));
  }

  public class Settings : CommonSettings {
    [CommandArgument(0, "<ACCOUNT_NAME>")] public string? AccountName { get; init; }
  }
}