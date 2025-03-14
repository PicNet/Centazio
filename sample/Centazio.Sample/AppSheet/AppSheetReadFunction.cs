using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Core.Stage;

namespace Centazio.Sample.AppSheet;

public class AppSheetReadFunction(IStagedEntityRepository stager, ICtlRepository ctl, AppSheetApi api) : ReadFunction(SampleConstants.Systems.AppSheet, stager, ctl) {

  protected override FunctionConfig GetFunctionConfiguration() => new([
    new ReadOperationConfig(SampleConstants.SystemEntities.AppSheet.Task, CronExpressionsHelper.EveryXSeconds(10), GetSheetTasks)
  ]);

  // AppSheet API does not have a 'last updated' so we just rely on Checksum to update records when required
  private async Task<ReadOperationResult> GetSheetTasks(OperationStateAndConfig<ReadOperationConfig> config) => CreateResult(await api.GetAllTasks());
}