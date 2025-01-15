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
    var tasks = await api.GetTasksAfter(config.Checkpoint);
    var last = tasks.LastOrDefault()?.LastUpdated;
    return CreateResult(tasks.Select(t => t.Json).ToList(), last);
  }
}