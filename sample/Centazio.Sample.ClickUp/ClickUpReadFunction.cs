using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Core.Stage;

namespace Centazio.Sample.ClickUp;

public class ClickUpReadFunction(IStagedEntityRepository stager, ICtlRepository ctl, ClickUpApi api) : ReadFunction(ClickUpConstants.ClickUpSystemName, stager, ctl) {

  protected override FunctionConfig GetFunctionConfiguration() => new([
    new ReadOperationConfig(ClickUpConstants.ClickUpTaskEntityName, CronExpressionsHelper.EveryXSeconds(5), GetUpdatedTasks)
  ]);

  private async Task<ReadOperationResult> GetUpdatedTasks(OperationStateAndConfig<ReadOperationConfig> config) {
    var tasks = await api.GetTasksAfter(config.Checkpoint);
    var last = tasks.LastOrDefault()?.LastUpdated;
    return CreateResult(tasks.Select(t => t.Json).ToList(), last);
  }
}