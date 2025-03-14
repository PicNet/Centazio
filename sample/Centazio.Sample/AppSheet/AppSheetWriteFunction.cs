using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;
using Centazio.Core.Write;

namespace Centazio.Sample.AppSheet;

public class AppSheetWriteFunction(SampleCoreStorageRepository core, ICtlRepository ctl, AppSheetApi api) : WriteFunction(SampleConstants.Systems.AppSheet, core, ctl) {
  
  public override FunctionConfig<WriteOperationConfig> GetFunctionConfiguration() => new([
    new WriteOperationConfig(SampleConstants.CoreEntities.Task, CronExpressionsHelper.EveryXSeconds(5), CovertCoreTasksToAppSheetTasks, WriteAppSheetTasks)
  ]);

  private Task<CovertCoresToSystemsResult> CovertCoreTasksToAppSheetTasks(WriteOperationConfig config, List<CoreAndPendingCreateMap> tocreate, List<CoreAndPendingUpdateMap> toupdate) => 
      Task.FromResult(CovertCoresToSystems<CoreTask>(tocreate, toupdate, (id, e) => AppSheetTask.Create(id.ToString(), e.Name, e.Completed)));
  
  private Task<WriteOperationResult> WriteAppSheetTasks(WriteOperationConfig config, List<CoreSystemAndPendingCreateMap> tocreate, List<CoreSystemAndPendingUpdateMap> toupdate) => 
      WriteOperationResult.Create<CoreTask, AppSheetTask>(tocreate, toupdate, AddNewTasks, EditTasks);

  private async Task<List<Map.Created>> AddNewTasks(List<CoreSystemAndPendingCreateMap<CoreTask, AppSheetTask>> tocreate) {
   var created = await api.AddTasks(tocreate.Select(t => t.CoreEntity.Name).ToList());
   return tocreate.Select((e, idx) => e.SuccessCreate(created[idx].SystemId)).ToList();
  }
  
  private async Task<List<Map.Updated>> EditTasks(List<CoreSystemAndPendingUpdateMap<CoreTask, AppSheetTask>> toupdate) {
    var sysents = toupdate.Select(t => AppSheetTask.Create(t.SystemEntity.SystemId, t.CoreEntity.Name, t.CoreEntity.Completed)).ToList();
    var (toedit, todelete) = (sysents.Where(t => !t.Completed).ToList(), sysents.Where(t => t.Completed).ToList());

    await Task.WhenAll(api.EditTasks(toedit), api.DeleteTasks(todelete));
    
    return toupdate.Select(e => e.SuccessUpdate()).ToList();
  }

}
