using Centazio.Core;
using Centazio.Core.Ctl;
using Centazio.Core.Misc;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Core.Stage;

namespace Centazio.Sample.GoogleSheets;

public class GoogleSheetsReadFunction(IStagedEntityRepository stager, ICtlRepository ctl, GoogleSheetsApi api) : ReadFunction(SampleConstants.Systems.GoogleSheets, stager, ctl) {

  protected override FunctionConfig<ReadOperationConfig> GetFunctionConfiguration() => new([
    new ReadOperationConfig(SampleConstants.SystemEntities.GoogleSheets.TaskRow, CronExpressionsHelper.EveryXSeconds(10), GetSheetTasks)
  ]);

  // Google Sheets API does not have a 'last updated' so we just rely on Checksum to update the single record when required
  private async Task<ReadOperationResult> GetSheetTasks(OperationStateAndConfig<ReadOperationConfig> config) {
    var rows = await api.GetSheetData();
    return CreateResult(rows.Select((task, idx) => Json.Serialize(new GoogleSheetsTaskRow(idx, task))).ToList());
  }

}