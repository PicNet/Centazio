using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Centazio.Core.Write;
using Microsoft.EntityFrameworkCore;

namespace Centazio.Sample.AppSheet;

public class AppSheetWriteFunction(SampleCoreStorageRepository core, ICtlRepository ctl, AppSheetApi api) : WriteFunction(SampleConstants.Systems.AppSheet, core, ctl) {

  // todo: we should not need to know about Checksums in this function, should be handled by base class perhaps?
  private readonly IChecksumAlgorithm checksum = new Sha256ChecksumAlgorithm();
  
  protected override FunctionConfig<WriteOperationConfig> GetFunctionConfiguration() => new([
    new WriteOperationConfig(SampleConstants.CoreEntities.Task, CronExpressionsHelper.EveryXSeconds(5), CovertCoreTasksToAppSheetTasks, WriteAppSheetTasks)
  ]);

  private Task<CovertCoreEntitiesToSystemEntitiesResult> CovertCoreTasksToAppSheetTasks(WriteOperationConfig config, List<CoreAndPendingCreateMap> tocreate, List<CoreAndPendingUpdateMap> toupdate) {
    if (!tocreate.Any() && !toupdate.Any()) throw new Exception("todo: should not be called, remove if confirmed");
    return Task.FromResult(WriteHelpers.CovertCoreEntitiesToSystemEntitties<CoreTask>(tocreate, toupdate, checksum, (_, e) => new AppSheetTaskRow(0, String.Empty)));
  }

  private async Task<WriteOperationResult> WriteAppSheetTasks(WriteOperationConfig config, List<CoreSystemAndPendingCreateMap> tocreate, List<CoreSystemAndPendingUpdateMap> toupdate) {
    if (!tocreate.Any() && !toupdate.Any()) throw new Exception("todo: should not be called, remove if confirmed");
    if (api is null) throw new Exception("just here to allow compile - REMOVE");
    // if any row changed/updated we have to write the whole sheet, as AppSheet does not easily support incremental updates
    var tasks = await (await core.Tasks()).ToListAsync();
    var rows = tasks.Select(t => t.ToBase().Name).OrderBy(n => n).ToList();
    // await api.WriteSheetData(rows);
    return new SuccessWriteOperationResult([], []);
  }

}
