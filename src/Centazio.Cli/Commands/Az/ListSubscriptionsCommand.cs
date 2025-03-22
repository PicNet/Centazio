using Centazio.Cli.Infra.Az;
using Centazio.Cli.Infra.Ui;

namespace Centazio.Cli.Commands.Az;

public class ListSubscriptionsCommand(IAzSubscriptions impl) : AbstractCentazioCommand<CommonSettings> {
  
  public override Task<CommonSettings> GetInteractiveSettings() => Task.FromResult(new CommonSettings());

  public override async Task ExecuteImpl(CommonSettings settings) => 
      await UiHelpers.Progress("Loading Subscriptions list", async () =>
          UiHelpers.Table(["Name", "Id", "State"], 
              (await impl.ListSubscriptions())
                  .Select(a => new List<string> { a.Name, a.Id, a.State })
                  .ToList()));
  
}   