using Centazio.Core;
using Centazio.Core.Ctl;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Core.Stage;

namespace Centazio.Sample;

public record DummySystemEntyity(Guid Id, string Name);

public class ClickUpReadFunction(IStagedEntityRepository stager, ICtlRepository ctl, ClickUpApi api) :
    ReadFunction(new(nameof(ClickUpReadFunction)), stager, ctl) {

  private readonly string EVERY_X_SECONDS_NCRON = "*/5 * * * * *";

  protected override FunctionConfig<ReadOperationConfig> GetFunctionConfiguration() => new([
    new ReadOperationConfig(Constants.CU_TASK, EVERY_X_SECONDS_NCRON, GetTaskUpdates)
  ]);

  private async Task<ReadOperationResult> GetTaskUpdates(OperationStateAndConfig<ReadOperationConfig> config) => 
      ReadOperationResult.Create(await api.GetTasksAfter(config.Checkpoint));

}