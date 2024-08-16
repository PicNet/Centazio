using System.Text.Json;
using Amazon;
using Amazon.Organizations;
using Amazon.Organizations.Model;
using Amazon.Runtime;
using Centazio.Core;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Aws;

public class AccountsCommand(CliSettings clisetts, CliSecrets secrets) : AbstractAwsCentazioCommand<AccountsCommand, AccountsCommand.AccountsCommandSettings>("accounts") {
  
  private readonly AccountsCommandImpl impl = new(secrets);
  
  public override Task<int> RunInteractiveCommand(CommandContext ctx) {
    var op = AnsiConsole.Prompt(new SelectionPrompt<string>()
        .Title("Select Operation:")
        .AddChoices(["list", "create"]));
    
    if (op == "list") return ExecuteAsync(ctx, new AccountsCommandSettings { List = true });
    return ExecuteAsync(ctx, new AccountsCommandSettings { 
      Create = op == "create",
      AccountName = AnsiConsole.Ask("Account Name:", clisetts.DefaultAccountName)  
    });
  }

  public override Task<int> ExecuteAsync(CommandContext context, AccountsCommandSettings settings) {
    if (settings.List) return ListAccounts();
    if (settings.Create) return CreateAccount(settings.AccountName);
    throw new Exception($"Invalid settings state: " + JsonSerializer.Serialize(settings));
  }

  private async Task<int> ListAccounts() {
    return await Progress("Loading account list", async () => {
      var table = new Table().AddColumns(["Name", "Id", "Arn", "Status", "Email"]);
      (await impl.ListAccounts())
          .ForEachIdx(a => table.AddRow([a.Name, a.Id, a.Arn, a.Status, a.Email]));
      AnsiConsole.Write(table);
      return 0;
    });
  }

  private async Task<int> CreateAccount(string name) => await ProgressWithErrorMessage("Loading account list", async () => await impl.CreateAccount(name));

  public class AccountsCommandSettings : CommonSettings {
    [CommandArgument(0, "<ACCOUNT_NAME>")] public string AccountName { get; init; } = null!;
    
    [CommandOption("-l|--list")] public bool List { get; init; }
    [CommandOption("-c|--create")] public bool Create { get; init; }
  }
  
  public class AccountsCommandImpl(CliSecrets secrets) {
    public async Task<string> CreateAccount(string name) {
      var response = await GetClient().CreateAccountAsync(new CreateAccountRequest { AccountName = name });
      var state = response.CreateAccountStatus.State;
      return state == CreateAccountState.SUCCEEDED 
          ? "" 
          : response.CreateAccountStatus.FailureReason.Value ?? "Unknown failure";  
    }
    
    public async Task<IEnumerable<(string Name, string Id, string Arn, AccountStatus Status, string Email)>> ListAccounts() => 
        (await GetClient().ListAccountsAsync(new ListAccountsRequest()))
            .Accounts.Select(a => (a.Name, a.Id, a.Arn, a.Status, a.Email));

    private AmazonOrganizationsClient GetClient() => new(
        new BasicAWSCredentials(secrets.AWS_KEY, secrets.AWS_SECRET), 
        new AmazonOrganizationsConfig { RegionEndpoint = RegionEndpoint.APSoutheast2 });

  }
}