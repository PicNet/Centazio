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
  
  public override Task<int> RunInteractiveCommand(CommandContext ctx) {
    var op = AnsiConsole.Prompt(new SelectionPrompt<string>()
        .Title("Select Operation:")
        .AddChoices(["list", "create", "delete"]));
    
    if (op == "list") return ExecuteAsync(ctx, new AccountsCommandSettings { List = true });
    return ExecuteAsync(ctx, new AccountsCommandSettings { 
      Create = op == "create",
      Delete = op == "delete",
      AccountName = AnsiConsole.Ask("Account Name:", clisetts.DefaultAccountName)  
    });
  }

  public override Task<int> ExecuteAsync(CommandContext context, AccountsCommandSettings settings) {
    if (settings.List) return ListAccounts();
    if (settings.Create) return CreateAccount(settings.AccountName);
    if (settings.Delete) return DeleteAccount(settings.AccountName);
    throw new Exception($"Invalid settings state: " + JsonSerializer.Serialize(settings));
  }

  private async Task<int> ListAccounts() {
    return await Progress("Loading account list", async () => {
      var response = await GetClient().ListAccountsAsync(new ListAccountsRequest());
      var table = new Table().AddColumns(["Name", "Id", "Arn", "Status", "Email"]);
      response.Accounts.ForEachIdx(a => table.AddRow([a.Name, a.Id, a.Arn, a.Status, a.Email]));
      AnsiConsole.Write(table);
      return 0;
    });
  }

  private async Task<int> CreateAccount(string name) {
    await GetClient().CreateAccountAsync(new CreateAccountRequest { AccountName = name });
    return 0;
  }

  private Task<int> DeleteAccount(string name) {
    if (!AnsiConsole.Prompt(new ConfirmationPrompt($"Are you sure you want to delete the account [{name}] and all children resources?"))) {
      return Task.FromResult(0);
    }

    return Task.FromResult(0);
  }
  
  private AmazonOrganizationsClient GetClient() {
    var client =  new AmazonOrganizationsClient(
        new BasicAWSCredentials(secrets.AWS_KEY, secrets.AWS_SECRET), 
        new AmazonOrganizationsConfig { RegionEndpoint = RegionEndpoint.APSoutheast2 });
    return client;
  }

  public class AccountsCommandSettings : CommonSettings {
    [CommandArgument(0, "<ACCOUNT_NAME>")] public string AccountName { get; set; } = null!;
    
    [CommandOption("-l|--list")] public bool List { get; set; } = false;
    [CommandOption("-c|--create")] public bool Create { get; set; } = false;
    [CommandOption("-d|--delete")] public bool Delete { get; set; } = false;
  }

}