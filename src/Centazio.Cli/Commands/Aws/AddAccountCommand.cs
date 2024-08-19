using Centazio.Cli.Infra;
using Centazio.Cli.Infra.Aws;
using Centazio.Cli.Infra.Ui;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Aws;

public class AddAccountCommand(CliSettings clisetts, IAwsAccounts impl) 
    : AbstractCentazioCommand<AddAccountCommand.CreateAccountCommandSettings> {
  
  protected override void RunInteractiveCommandImpl() => 
      _ = ExecuteImpl(new CreateAccountCommandSettings { AccountName = UiHelpers.Ask("Account Name", clisetts.DefaultAccountName) });

  protected override async Task ExecuteImpl(CreateAccountCommandSettings settings) {
    if (String.IsNullOrWhiteSpace(settings.AccountName)) throw new Exception(Interactive ? "Account Name is required" : "<ACCOUNT_NAME> is required");
    await UiHelpers.ProgressWithErrorMessage("Creating account", async () => await impl.CreateAccount(settings.AccountName));
  }

  public class CreateAccountCommandSettings : CommonSettings {
    [CommandArgument(0, "<ACCOUNT_NAME>")] public string? AccountName { get; init; }
  }
}