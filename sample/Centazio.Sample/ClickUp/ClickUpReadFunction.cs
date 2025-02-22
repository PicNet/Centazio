using Centazio.Core;
using Centazio.Core.Ctl;
using Centazio.Core.Misc;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Core.Settings;
using Centazio.Core.Stage;

namespace Centazio.Sample.ClickUp;

public class ClickUpReadFunction(IStagedEntityRepository stager, ICtlRepository ctl, ClickUpApi api, CentazioSettings settings) : ReadFunction(SampleConstants.Systems.ClickUp, stager, ctl, settings) {

  protected override FunctionConfig<ReadOperationConfig> GetFunctionConfiguration() => new([
    new ReadOperationConfig(SampleConstants.SystemEntities.ClickUp.Task, CronExpressionsHelper.EveryXSeconds(5), GetUpdatedTasks)
  ]);

  private async Task<ReadOperationResult> GetUpdatedTasks(OperationStateAndConfig<ReadOperationConfig> config) {
    var tasks = await api.GetTasksAfter(config.Checkpoint);
    var last = tasks.LastOrDefault()?.LastUpdated;
    return CreateResult(tasks.Select(t => t.Json).ToList(), last);
  }
}