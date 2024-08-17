using System.Text.Json;
using Amazon;
using Amazon.Organizations;
using Amazon.Organizations.Model;
using Amazon.Runtime;
using Centazio.Cli.Infra;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Centazio.Cli.Commands.Aws;

public class AccountsCommand(CliSettings clisetts, CliSecrets secrets) 
    : AbstractCentazioCommand<AccountsCommand, AccountsCommand.AccountsCommandSettings>("accounts") {
  
  private readonly AccountsCommandImpl impl = new(secrets);
  
  protected override bool RunInteractiveCommandImpl() {
    switch (PromptCommandOptions(["list", "create"])) {
      case "back": return false;
      case "list": ExecuteImpl(new AccountsCommandSettings { List = true });
        break;
      case "create": ExecuteImpl(new AccountsCommandSettings { Create = true, AccountName = Ask("Account Name", clisetts.DefaultAccountName) });
        break;
      default: throw new Exception();
    }
    return true;
  }

  protected override void ExecuteImpl(AccountsCommandSettings settings) {
    if (settings.List) ListAccounts();
    else if (settings.Create) {
      if (String.IsNullOrWhiteSpace(settings.AccountName)) throw new Exception(Interactive ? "Account Name is required" : "<ACCOUNT_NAME> is required");
      // CreateAccount(settings.AccountName);
    } else throw new Exception($"Invalid settings state: " + JsonSerializer.Serialize(settings));
  }

  private async void ListAccounts() => 
      await Progress("Loading account list", async () => 
          AnsiConsole.Write(new Table()
              .AddColumns(["Name", "Id", "Arn", "Status", "Email"])
              .AddRows((await impl.ListAccounts()).Select(a => new [] { a.Name, a.Id, a.Arn, a.Status, a.Email }))));

  private void CreateAccount(string name) => ProgressWithErrorMessage("Loading account list", async () => await impl.CreateAccount(name));

  public class AccountsCommandSettings : CommonSettings {
    [CommandArgument(0, "<ACCOUNT_NAME>")] public string? AccountName { get; init; }
    
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
    
    public async Task<IEnumerable<(string Id, string Name, string Arn, string Status, string Email)>> ListAccounts() => 
        (await GetClient().ListAccountsAsync(new ListAccountsRequest()))
            .Accounts.Select(a => (a.Id, a.Name, a.Arn, a.Status.Value, a.Email));

    private AmazonOrganizationsClient GetClient() => new(
        new BasicAWSCredentials(secrets.AWS_KEY, secrets.AWS_SECRET), 
        new AmazonOrganizationsConfig { RegionEndpoint = RegionEndpoint.APSoutheast2 });

  }
}