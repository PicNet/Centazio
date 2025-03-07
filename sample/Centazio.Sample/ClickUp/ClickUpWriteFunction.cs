﻿using Centazio.Core;
using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Misc;
using Centazio.Core.Runner;
using Centazio.Core.Settings;
using Centazio.Core.Write;

namespace Centazio.Sample.ClickUp;

public class ClickUpWriteFunction(ICoreStorage core, ICtlRepository ctl, ClickUpApi api, CentazioSettings settings) : WriteFunction(SampleConstants.Systems.ClickUp, core, ctl, settings) {
  
  public override FunctionConfig<WriteOperationConfig> GetFunctionConfiguration() => new([
    new WriteOperationConfig(SampleConstants.CoreEntities.Task, CronExpressionsHelper.EveryXSeconds(5), CovertCoreTasksToClickUpTasks, WriteClickUpTasks)
  ]);

  private Task<CovertCoresToSystemsResult> CovertCoreTasksToClickUpTasks(WriteOperationConfig config, List<CoreAndPendingCreateMap> tocreate, List<CoreAndPendingUpdateMap> toupdate) =>
      Task.FromResult(CovertCoresToSystems<CoreTask>(tocreate, toupdate, (id, e) => new ClickUpTask(id.Value, e.Name, new(SampleConstants.Misc.CLICK_UP_OPEN_STATUS), UtcDate.ToMillis().ToString())));

  private Task<WriteOperationResult> WriteClickUpTasks(WriteOperationConfig config, List<CoreSystemAndPendingCreateMap> tocreate, List<CoreSystemAndPendingUpdateMap> toupdate) => 
      WriteOperationResult.Create<CoreTask, ClickUpTask>(tocreate, toupdate, CreateTasks, UpdateTasks);

  private async Task<List<Map.Created>> CreateTasks<C, S>(List<CoreSystemAndPendingCreateMap<C, S>> tocreate) where C : ICoreEntity where S : ISystemEntity {
    var newids = await Task.WhenAll(tocreate.Select(async sysent => await api.CreateTask(sysent.SystemEntity.To<ClickUpTask>().name)));
    return newids.Select((id, idx) => tocreate[idx].SuccessCreate(new (id))).ToList();
  }

  private async Task<List<Map.Updated>> UpdateTasks<C, S>(List<CoreSystemAndPendingUpdateMap<C, S>> toupdate) where C : ICoreEntity where S : ISystemEntity {
    var updated = await Task.WhenAll(toupdate.Select(async sysent => {
      var task = sysent.SystemEntity.To<ClickUpTask>();
      await api.UpdateTask(task.id, task.name);
      return task;
    }));
    return updated.Select((_, idx) => toupdate[idx].SuccessUpdate()).ToList();
  }
}

