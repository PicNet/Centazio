using Centazio.Cli.Infra.Az;
using Centazio.Cli.Infra.Ui;

namespace Centazio.Cli.Commands.Az;

public class ListResourceGroupsCommand(IAzResourceGroups impl) : AbstractCentazioCommand<CommonSettings> {
  
  protected override Task<CommonSettings> GetInteractiveSettings() => Task.FromResult(new CommonSettings());

  protected override async Task ExecuteImpl(CommonSettings settings) => 
      await UiHelpers.Progress("Loading ResourceGroup list", async () =>
          UiHelpers.Table(["Name", "Id", "State", "ManagedBy"], 
              (await impl.ListResourceGroups()).Select(a => new List<string> { a.Name, a.Id, a.State, a.ManagedBy }).ToList()));
  
}