using System.Text.Json;
using Centazio.Cli.Infra;
using Centazio.Cli.Infra.Aws;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Aws;

public class AccountsCommand(CliSettings clisetts, IAwsAccounts impl) 
    : AbstractCentazioCommand<AccountsCommand.AccountsCommandSettings>("accounts") {
  
  protected override bool RunInteractiveCommandImpl() {
    switch (PromptCommandOptions(["list", "create"])) {
      case "back": return false;
      case "list": _ = ExecuteImpl(new AccountsCommandSettings { List = true });
        break;
      case "create": _ = ExecuteImpl(new AccountsCommandSettings { Create = true, AccountName = Ask("Account Name", clisetts.DefaultAccountName) });
        break;
      default: throw new Exception();
    }
    return true;
  }

  protected override async Task ExecuteImpl(AccountsCommandSettings settings) {
    if (settings.List) await ListAccounts();
    else if (settings.Create) {
      if (String.IsNullOrWhiteSpace(settings.AccountName)) throw new Exception(Interactive ? "Account Name is required" : "<ACCOUNT_NAME> is required");
      // await CreateAccount(settings.AccountName);
    } else throw new Exception($"Invalid settings state: " + JsonSerializer.Serialize(settings));
  }

  private async Task ListAccounts() => 
      await Progress("Loading account list", async () => 
          AnsiConsole.Write(new Table()
              .AddColumns(["Name", "Id", "Arn", "Status", "Email"])
              .AddRows((await impl.ListAccounts()).Select(a => new [] { a.Name, a.Id, a.Arn, a.Status, a.Email }))));

  private async Task CreateAccount(string name) => await ProgressWithErrorMessage("Loading account list", async () => await impl.CreateAccount(name));

  public class AccountsCommandSettings : CommonSettings {
    [CommandArgument(0, "<ACCOUNT_NAME>")] public string? AccountName { get; init; }
    
    [CommandOption("-l|--list")] public bool List { get; init; }
    [CommandOption("-c|--create")] public bool Create { get; init; }
  }
}