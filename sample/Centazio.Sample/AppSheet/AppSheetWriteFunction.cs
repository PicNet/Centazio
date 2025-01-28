using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Centazio.Core.Write;

namespace Centazio.Sample.AppSheet;

// todo: sample has to handle completed tasks (remove from AppSheet)
public class AppSheetWriteFunction(SampleCoreStorageRepository core, ICtlRepository ctl, AppSheetApi api) : WriteFunction(SampleConstants.Systems.AppSheet, core, ctl) {

  // todo: we should not need to know about Checksums in this function, should be handled by base class perhaps?
  private readonly IChecksumAlgorithm checksum = new Sha256ChecksumAlgorithm();
  
  protected override FunctionConfig<WriteOperationConfig> GetFunctionConfiguration() => new([
    new WriteOperationConfig(SampleConstants.CoreEntities.Task, CronExpressionsHelper.EveryXSeconds(5), CovertCoreTasksToAppSheetTasks, WriteAppSheetTasks)
  ]);

  private Task<CovertCoreEntitiesToSystemEntitiesResult> CovertCoreTasksToAppSheetTasks(WriteOperationConfig config, List<CoreAndPendingCreateMap> tocreate, List<CoreAndPendingUpdateMap> toupdate) {
    if (!tocreate.Any() && !toupdate.Any()) throw new Exception("todo: should not be called, remove if confirmed");
    return Task.FromResult(WriteHelpers.CovertCoreEntitiesToSystemEntitties<CoreTask>(tocreate, toupdate, checksum, (id, e) => AppSheetTask.Create(id.ToString(), e.Name)));
  }

  private async Task<WriteOperationResult> WriteAppSheetTasks(WriteOperationConfig config, List<CoreSystemAndPendingCreateMap> tocreate, List<CoreSystemAndPendingUpdateMap> toupdate) {
    if (!tocreate.Any() && !toupdate.Any()) throw new Exception("todo: should not be called, remove if confirmed");
    return new SuccessWriteOperationResult(await AddNewTasks(tocreate), await EditTasks(toupdate));
  }
  
  private async Task<List<Map.Created>> AddNewTasks(List<CoreSystemAndPendingCreateMap> tocreate) {
   if (!tocreate.Any()) return [];
   var created = await api.AddTasks(tocreate.Select(t => t.CoreEntity.To<CoreTask>().Name).ToList());
   return tocreate.Select((e, idx) => e.Map.SuccessCreate(created[idx].SystemId, checksum.Checksum(created[idx]))).ToList();
  }
  
  private async Task<List<Map.Updated>> EditTasks(List<CoreSystemAndPendingUpdateMap> toupdate) {
    if (!toupdate.Any()) return [];
    // todo: this is weird behaviour, and not intuitive/discoverable
    var aptasks = toupdate.Select(t => AppSheetTask.Create(t.SystemEntity.SystemId, t.CoreEntity.To<CoreTask>().Name)).ToList(); 
    var edited = await api.EditTasks(aptasks);
    return toupdate.Select((e, idx) => e.Map.SuccessUpdate(checksum.Checksum(edited[idx]))).ToList();
  }

}
