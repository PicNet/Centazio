using Centazio.Cli.Infra.Aws;
using Centazio.Cli.Infra.Ui;

namespace Centazio.Cli.Commands.Aws;

public class ListAccountsCommand(IAwsAccounts impl) : AbstractCentazioCommand<CommonSettings> {
  
  public override Task<CommonSettings> GetInteractiveSettings() => Task.FromResult(new CommonSettings());

  public override async Task ExecuteImpl(CommonSettings settings) => 
      await UiHelpers.Progress("Loading account list", async () => 
          UiHelpers.Table(["Name", "Id", "Arn", "Status", "Email"], 
              (await impl.ListAccounts())
                  .Select(a => new List<string> { a.Name, a.Id, a.Arn, a.Status, a.Email })
                  .ToList()));
}