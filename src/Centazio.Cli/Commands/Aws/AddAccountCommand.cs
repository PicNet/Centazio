using Centazio.Cli.Infra;
using Centazio.Cli.Infra.Aws;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Aws;

public class AddAccountCommand(CliSettings clisetts, IAwsAccounts impl) 
    : AbstractCentazioCommand<AddAccountCommand.CreateAccountCommandSettings> {
  
  protected override bool RunInteractiveCommandImpl() {
    _ = ExecuteImpl(new CreateAccountCommandSettings { AccountName = Ask("Account Name", clisetts.DefaultAccountName) });
    return true;
  }

  protected override async Task ExecuteImpl(CreateAccountCommandSettings settings) {
    if (String.IsNullOrWhiteSpace(settings.AccountName)) throw new Exception(Interactive ? "Account Name is required" : "<ACCOUNT_NAME> is required");
    await ProgressWithErrorMessage("Creating account", async () => await impl.CreateAccount(settings.AccountName));
  }

  public class CreateAccountCommandSettings : CommonSettings {
    [CommandArgument(0, "<ACCOUNT_NAME>")] public string? AccountName { get; init; }
  }
}