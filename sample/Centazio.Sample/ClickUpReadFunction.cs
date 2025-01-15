using System.Text.RegularExpressions;
using Centazio.Core;
using Centazio.Core.Ctl;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Core.Stage;

namespace Centazio.Sample;

public class ClickUpReadFunction(IStagedEntityRepository stager, ICtlRepository ctl, ClickUpApi api) : ReadFunction(Constants.CLICK_UP, stager, ctl) {

  private readonly string EVERY_X_SECONDS_NCRON = "*/5 * * * * *";

  protected override FunctionConfig<ReadOperationConfig> GetFunctionConfiguration() => new([
    new ReadOperationConfig(Constants.CU_TASK, EVERY_X_SECONDS_NCRON, GetUpdatedTasks)
  ]);

  private async Task<ReadOperationResult> GetUpdatedTasks(OperationStateAndConfig<ReadOperationConfig> config) {
    // todo: move date extraction logic out, and make filtering LastUpdate > Checkpoint logic reuseable
    var tasks = await api.GetTasksAfter(config.Checkpoint);
    var wupdates = tasks
        .Select(json => new TaskWithLastUpdate(json, DateTimeOffset.FromUnixTimeMilliseconds(Int64.Parse(Regex.Match(json, @"""date_updated"":""([^""]+)""").Groups[1].Value)).DateTime))
        // it is possible for the ClickUp API to include some tasks even though we specify date_updated_gt
        .Where(t => t.LastUpdate > config.Checkpoint)
        .OrderBy(t => t.LastUpdate)
        .ToList();
    var last = wupdates.LastOrDefault()?.LastUpdate;
    return CreateResult(wupdates.Select(t => t.TaskJson).ToList(), last);
  }

  private record TaskWithLastUpdate(string TaskJson, DateTime LastUpdate);
}