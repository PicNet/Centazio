﻿using Centazio.Core.Promote;
using Centazio.Core.Runner;
using Centazio.Core.Stage;

namespace Centazio.Sample.AppSheet;

public class AppSheetPromoteFunction(IStagedEntityRepository stager, ICoreStorage corestg, ICtlRepository ctl) : PromoteFunction(AppSheetConstants.AppSheetSystemName, stager, corestg, ctl) {

  protected override FunctionConfig GetFunctionConfiguration() => new([
    new PromoteOperationConfig(System, typeof(AppSheetTask), AppSheetConstants.AppSheetTaskEntityName, CoreEntityTypes.Task, CronExpressionsHelper.EveryXSeconds(5), PromoteTasks) 
  ]);

  private Task<List<EntityEvaluationResult>> PromoteTasks(OperationStateAndConfig<PromoteOperationConfig> config, List<EntityForPromotionEvaluation> toeval) {
    var results  = toeval.Select(eval => {
      var astask = eval.SystemEntity.To<AppSheetTask>();
      if (String.IsNullOrWhiteSpace(astask.Task)) { return eval.MarkForIgnore(new($"AppSheet Task[{astask.RowId}] has a null or empty task name and will not be promoted")); }
      
      var task = (eval.ExistingCoreEntityAndMeta?.As<CoreTask>() ?? new CoreTask(new(Guid.CreateVersion7().ToString()), String.Empty, false)) with { Name = astask.Task ?? throw new Exception(), Completed = false };
      return eval.MarkForPromotion(task);
    }).ToList();
    return Task.FromResult(results);
  }

}
