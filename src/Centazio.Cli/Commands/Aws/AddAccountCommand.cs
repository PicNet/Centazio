using Centazio.Cli.Infra;
using Centazio.Cli.Infra.Aws;
using Centazio.Cli.Infra.Ui;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Aws;

public class AddAccountCommand(CliSettings clisetts, IAwsAccounts impl) 
    : AbstractCentazioCommand<AddAccountCommand.AddAccountCommandSettings> {
  
  protected override Task RunInteractiveCommandImpl() => 
      ExecuteImpl(new AddAccountCommandSettings { AccountName = UiHelpers.Ask("Account Name", clisetts.DefaultAccountName) });

  protected override async Task ExecuteImpl(AddAccountCommandSettings settings) {
    if (String.IsNullOrWhiteSpace(settings.AccountName)) throw new Exception(Interactive ? "Account Name is required" : "<ACCOUNT_NAME> is required");
    await UiHelpers.ProgressWithErrorMessage("Creating account", async () => await impl.AddAccount(settings.AccountName));
  }

  public class AddAccountCommandSettings : CommonSettings {
    [CommandArgument(0, "<ACCOUNT_NAME>")] public string? AccountName { get; init; }
  }
}