using Centazio.Cli.Infra;
using Centazio.Cli.Infra.Aws;
using Centazio.Cli.Infra.Ui;
using Spectre.Console;

namespace Centazio.Cli.Commands.Aws;

public class ListAccountsCommand(IAwsAccounts impl) : AbstractCentazioCommand<CommonSettings> {
  
  protected override Task<CommonSettings> GetInteractiveSettings() => Task.FromResult(new CommonSettings());

  protected override async Task ExecuteImpl(string name, CommonSettings settings) => 
      await UiHelpers.Progress("Loading account list", async () => 
          AnsiConsole.Write(new Table()
              .AddColumns(["Name", "Id", "Arn", "Status", "Email"])
              .AddRows((await impl.ListAccounts())
                  .Select(a => new [] { a.Name, a.Id, a.Arn, a.Status, a.Email })
                  .ToList())));
}