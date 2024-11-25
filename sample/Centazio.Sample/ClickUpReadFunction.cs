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
    new ReadOperationConfig(Constants.CU_TASK, EVERY_X_SECONDS_NCRON, GetTaskUpdates)
  ]);

  private async Task<ReadOperationResult> GetTaskUpdates(OperationStateAndConfig<ReadOperationConfig> config) {
    var tasks = await api.GetTasksAfter(config.Checkpoint);
    var update_dts = Regex.Matches(tasks, @"""date_updated"":""([^""]+)""").Select(m => Int64.Parse(m.Groups[1].Value)).ToList();
    if (!update_dts.Any()) return ReadOperationResult.EmptyResult();
    
    return ReadOperationResult.Create(tasks, DateTimeOffset.FromUnixTimeMilliseconds(update_dts.Last()).DateTime);
  }

}