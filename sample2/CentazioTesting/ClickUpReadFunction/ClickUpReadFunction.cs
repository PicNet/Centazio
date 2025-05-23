using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Core.Stage;

namespace CentazioTesting.ClickUp;

public class ClickUpReadFunction(IStagedEntityRepository stager, ICtlRepository ctl, ClickUpApi api) : ReadFunction(ClickUpConstants.ClickUpSystemName, stager, ctl) {

  protected override FunctionConfig GetFunctionConfiguration() => new([
    new ReadOperationConfig(ClickUpConstants.ClickUpExampleEntityName, CronExpressionsHelper.EveryXSeconds(5), GetUpdatedTasks)
  ]);

  private async Task<ReadOperationResult> GetUpdatedTasks(OperationStateAndConfig<ReadOperationConfig> config) {
    var entities = await api.GetExampleEntities(config.Checkpoint);        
    return CreateResult(entities.Select(e => Json.Serialize(e)).ToList());
  }
}