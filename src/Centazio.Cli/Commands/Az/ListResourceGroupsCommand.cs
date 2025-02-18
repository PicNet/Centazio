using Centazio.Cli.Infra;
using Centazio.Cli.Infra.Az;
using Centazio.Cli.Infra.Ui;
using Spectre.Console;

namespace Centazio.Cli.Commands.Az;

public class ListResourceGroupsCommand(IAzResourceGroups impl) : AbstractCentazioCommand<CommonSettings> {
  
  protected override Task<CommonSettings> GetInteractiveSettings() => Task.FromResult(new CommonSettings());

  protected override async Task ExecuteImpl(CommonSettings settings) => 
      await UiHelpers.Progress("Loading ResourceGroup list", async () => 
          AnsiConsole.Write(new Table()
              .AddColumns(["Name", "Id", "State", "ManagedBy"])
              .AddRows((await impl.ListResourceGroups())
                  .Select(a => new [] { a.Name, a.Id, a.State, a.ManagedBy })
                  .ToList())));
  
}