using Centazio.Core;
using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Centazio.Core.Write;

namespace Centazio.Sample.AppSheet;

public class AppSheetWriteFunction(SampleCoreStorageRepository core, ICtlRepository ctl, AppSheetApi api) : WriteFunction(SampleConstants.Systems.AppSheet, core, ctl) {
  
  protected override FunctionConfig<WriteOperationConfig> GetFunctionConfiguration() => new([
    new WriteOperationConfig(SampleConstants.CoreEntities.Task, CronExpressionsHelper.EveryXSeconds(5), CovertCoreTasksToAppSheetTasks, WriteAppSheetTasks)
  ]);

  private Task<CovertCoreEntitiesToSystemEntitiesResult> CovertCoreTasksToAppSheetTasks(WriteOperationConfig config, List<CoreAndPendingCreateMap> tocreate, List<CoreAndPendingUpdateMap> toupdate) => 
      Task.FromResult(CovertCoreEntitiesToSystemEntitties<CoreTask>(tocreate, toupdate, (id, e) => AppSheetTask.Create(id.ToString(), e.Name, e.Completed)));
  
  private async Task<WriteOperationResult> WriteAppSheetTasks(WriteOperationConfig config, List<CoreSystemAndPendingCreateMap> tocreate, List<CoreSystemAndPendingUpdateMap> toupdate) => 
      new SuccessWriteOperationResult(await AddNewTasks(tocreate), await EditTasks(toupdate));

  private async Task<List<Map.Created>> AddNewTasks(List<CoreSystemAndPendingCreateMap> tocreate) {
   if (!tocreate.Any()) return [];
   var created = await api.AddTasks(tocreate.Select(t => t.CoreEntity.To<CoreTask>().Name).ToList());
   return tocreate.Select((e, idx) => e.SuccessCreate(created[idx].SystemId)).ToList();
  }
  
  private async Task<List<Map.Updated>> EditTasks(List<CoreSystemAndPendingUpdateMap> toupdate) {
    if (!toupdate.Any()) return [];
    // todo: having to call `CoreEntity.To<CoreTask>()` everywhere is a pain, need a more "generics" way of doing this 
    var sysents = toupdate.Select(t => {
      var task = t.CoreEntity.To<CoreTask>();
      return AppSheetTask.Create(t.SystemEntity.SystemId, task.Name, task.Completed);
    }).ToList();
    var (toedit, todelete) = (sysents.Where(t => !t.Completed).ToList(), sysents.Where(t => t.Completed).ToList());

    await Task.WhenAll(api.EditTasks(toedit), api.DeleteTasks(todelete));
    
    return toupdate.Select(e => e.SuccessUpdate()).ToList();
  }

}
