using Centazio.Core;
using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Centazio.Core.Write;

namespace Centazio.Sample.AppSheet;

// todo: sample has to handle completed tasks (remove from AppSheet)
public class AppSheetWriteFunction(SampleCoreStorageRepository core, ICtlRepository ctl, AppSheetApi api) : WriteFunction(SampleConstants.Systems.AppSheet, core, ctl) {
  
  
  protected override FunctionConfig<WriteOperationConfig> GetFunctionConfiguration() => new([
    new WriteOperationConfig(SampleConstants.CoreEntities.Task, CronExpressionsHelper.EveryXSeconds(5), CovertCoreTasksToAppSheetTasks, WriteAppSheetTasks)
  ]);

  private Task<CovertCoreEntitiesToSystemEntitiesResult> CovertCoreTasksToAppSheetTasks(WriteOperationConfig config, List<CoreAndPendingCreateMap> tocreate, List<CoreAndPendingUpdateMap> toupdate) => 
      Task.FromResult(CovertCoreEntitiesToSystemEntitties<CoreTask>(tocreate, toupdate, (id, e) => AppSheetTask.Create(id.ToString(), e.Name)));
  
  private async Task<WriteOperationResult> WriteAppSheetTasks(WriteOperationConfig config, List<CoreSystemAndPendingCreateMap> tocreate, List<CoreSystemAndPendingUpdateMap> toupdate) => 
      new SuccessWriteOperationResult(await AddNewTasks(tocreate), await EditTasks(toupdate));

  private async Task<List<Map.Created>> AddNewTasks(List<CoreSystemAndPendingCreateMap> tocreate) {
   if (!tocreate.Any()) return [];
   var created = await api.AddTasks(tocreate.Select(t => t.CoreEntity.To<CoreTask>().Name).ToList());
   return tocreate.Select((e, idx) => e.SuccessCreate(created[idx])).ToList();
  }
  
  private async Task<List<Map.Updated>> EditTasks(List<CoreSystemAndPendingUpdateMap> toupdate) {
    if (!toupdate.Any()) return [];
    var aptasks = toupdate.Select(t => AppSheetTask.Create(t.SystemEntity.SystemId, t.CoreEntity.To<CoreTask>().Name)).ToList(); 
    await api.EditTasks(aptasks);
    return toupdate.Select(e => e.SuccessUpdate()).ToList();
  }

}
