using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Centazio.Core.Write;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Sample.GoogleSheets;

public class GoogleSheetsWriteFunction(SampleCoreStorageRepository core, ICtlRepository ctl, GoogleSheetsApi api) : WriteFunction(SampleConstants.Systems.GoogleSheets, core, ctl) {

  // todo: we should not need to know about Checksums in this function, should be handled by base class perhaps?
  private readonly IChecksumAlgorithm checksum = new Sha256ChecksumAlgorithm();
  
  protected override FunctionConfig<WriteOperationConfig> GetFunctionConfiguration() => new([
    new WriteOperationConfig(SampleConstants.CoreEntities.Task, CronExpressionsHelper.EveryXSeconds(5), CovertCoreTasksToGoogleSheetsTasks, WriteGoogleSheetsTasks)
  ]);

  private Task<CovertCoreEntitiesToSystemEntitiesResult> CovertCoreTasksToGoogleSheetsTasks(WriteOperationConfig config, List<CoreAndPendingCreateMap> tocreate, List<CoreAndPendingUpdateMap> toupdate) {
    if (!tocreate.Any() && !toupdate.Any()) throw new Exception("todo: should not be called, remove if confirmed");
    return Task.FromResult(WriteHelpers.CovertCoreEntitiesToSystemEntitties<CoreTask>(tocreate, toupdate, checksum, (_, e) => new GoogleSheetsTaskRow(0, String.Empty)));
  }

  private async Task<WriteOperationResult> WriteGoogleSheetsTasks(WriteOperationConfig config, List<CoreSystemAndPendingCreateMap> tocreate, List<CoreSystemAndPendingUpdateMap> toupdate) {
    if (!tocreate.Any() && !toupdate.Any()) throw new Exception("todo: should not be called, remove if confirmed");
    // if any row changed/updated we have to write the whole sheet, as Google Sheets does not easily support incremental updates
    var tasks = await (await core.Tasks()).ToListAsync();
    var rows = tasks.Select(t => t.ToBase().Name).OrderBy(n => n).ToList();
    await api.WriteSheetData(rows);
    return new SuccessWriteOperationResult([], []);
  }

}
