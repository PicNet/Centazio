using Centazio.Core;
using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Centazio.Core.Write;

namespace Centazio.Sample.ClickUp;

public class ClickUpWriteFunction(ICoreStorage core, ICtlRepository ctl, ClickUpApi api) : WriteFunction(SampleConstants.Systems.ClickUp, core, ctl) {
  
  protected override FunctionConfig<WriteOperationConfig> GetFunctionConfiguration() => new([
    new WriteOperationConfig(SampleConstants.CoreEntities.Task, CronExpressionsHelper.EveryXSeconds(5), CovertCoreTasksToClickUpTasks, WriteClickUpTasks)
  ]);

  private Task<CovertCoreEntitiesToSystemEntitiesResult> CovertCoreTasksToClickUpTasks(WriteOperationConfig config, List<CoreAndPendingCreateMap> tocreate, List<CoreAndPendingUpdateMap> toupdate) =>
      Task.FromResult(CovertCoreEntitiesToSystemEntitties<CoreTask>(tocreate, toupdate, (id, e) => new ClickUpTask(id.Value, e.Name, UtcDate.ToMillis().ToString())));

  private async Task<WriteOperationResult> WriteClickUpTasks(WriteOperationConfig config, List<CoreSystemAndPendingCreateMap> tocreate, List<CoreSystemAndPendingUpdateMap> toupdate) {
    var createdids = await Task.WhenAll(tocreate.Select(async sysent => await api.CreateTask(sysent.SystemEntity.To<ClickUpTask>().name)));
    var updated = await Task.WhenAll(toupdate.Select(async sysent => {
      var task = sysent.SystemEntity.To<ClickUpTask>();
      await api.UpdateTask(task.id, task.name);
      return task;
    }));
    
    // todo: it was simpler to create this result object than use the WriteHelper, consider removing `WriteHelpers.GetSuccessWriteOperationResult`
    return new SuccessWriteOperationResult(
          createdids.Select((id, idx) => tocreate[idx].Map.SuccessCreate(new(id), Config.ChecksumAlgorithm.Checksum(tocreate[idx].SystemEntity))).ToList(), 
          updated.Select((sysent, idx) => toupdate[idx].Map.SuccessUpdate(Config.ChecksumAlgorithm.Checksum(sysent))).ToList());
  }

}
