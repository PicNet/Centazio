﻿using Centazio.Core;
using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl;
using Centazio.Core.Misc;
using Centazio.Core.Promote;
using Centazio.Core.Runner;
using Centazio.Core.Stage;

namespace Centazio.Sample.ClickUp;

public class ClickUpPromoteFunction(IStagedEntityRepository stager, ICoreStorage corestg, ICtlRepository ctl) : PromoteFunction(SampleConstants.Systems.ClickUp, stager, corestg, ctl) {
  
  protected override FunctionConfig<PromoteOperationConfig> GetFunctionConfiguration() => new([
    new PromoteOperationConfig(typeof(ClickUpTask), SampleConstants.SystemEntities.ClickUp.Task, SampleConstants.CoreEntities.Task, CronExpressionsHelper.EveryXSeconds(5), PromoteTasks) 
  ]);

  private Task<List<EntityEvaluationResult>> PromoteTasks(OperationStateAndConfig<PromoteOperationConfig> config, List<EntityForPromotionEvaluation> toeval) {
    var results  = toeval.Select(eval => {
      var source = eval.SystemEntity.To<ClickUpTask>();
      var target = (eval.ExistingCoreEntityAndMeta?.As<CoreTask>() ?? new CoreTask(new(Guid.CreateVersion7().ToString()), String.Empty)) with { Name = source.name };
      return eval.MarkForPromotion(target);
    }).ToList();
    return Task.FromResult(results);
  }

}
