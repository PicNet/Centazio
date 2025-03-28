using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Core.Stage;

namespace {{ it.Namespace }};

public class {{ it.SystemName }}ReadFunction(IStagedEntityRepository stager, ICtlRepository ctl, {{ it.SystemName }}Api api) : ReadFunction({{ it.SystemName }}Constants.{{ it.SystemName }}SystemName, stager, ctl) {

  protected override FunctionConfig GetFunctionConfiguration() => new([
    new ReadOperationConfig({{ it.SystemName }}Constants.{{ it.SystemName }}ExampleEntityName, CronExpressionsHelper.EveryXSeconds(5), GetUpdatedTasks)
  ]);

  private async Task<ReadOperationResult> GetUpdatedTasks(OperationStateAndConfig<ReadOperationConfig> config) {
    var entities = await api.GetExampleEntities(config.Checkpoint);        
    return CreateResult(entities.Select(e => Json.Serialize(e)).ToList());
  }
}