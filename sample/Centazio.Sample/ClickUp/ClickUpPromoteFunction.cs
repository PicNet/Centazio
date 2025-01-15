﻿using Centazio.Core;
using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl;
using Centazio.Core.Promote;
using Centazio.Core.Runner;
using Centazio.Core.Stage;

namespace Centazio.Sample.ClickUp;

public class ClickUpPromoteFunction(IStagedEntityRepository stager, ICoreStorage corestg, ICtlRepository ctl) : PromoteFunction(Constants.CLICK_UP, stager, corestg, ctl) {

  // todo: add reuseable ncron helpers somewhere
  private readonly string EVERY_X_SECONDS_NCRON = "*/5 * * * * *";
  
  protected override FunctionConfig<PromoteOperationConfig> GetFunctionConfiguration() => new([
    new PromoteOperationConfig(typeof(ClickUpTask), Constants.CU_TASK, Constants.TASK, EVERY_X_SECONDS_NCRON, PromoteTasks) 
  ]);

  private Task<List<EntityEvaluationResult>> PromoteTasks(OperationStateAndConfig<PromoteOperationConfig> config, List<EntityForPromotionEvaluation> toeval) {
    var results  = toeval.Select(eval => {
      var cutask = eval.SystemEntity.To<ClickUpTask>();
      var task = eval.ExistingCoreEntityAndMeta?.As<CoreTask>() ?? new CoreTask(new(Guid.CreateVersion7().ToString()), cutask.name);
      // todo: users should not need to know about the checksum algorithm here
      return eval.MarkForPromotion(eval, Constants.CLICK_UP, task, config.FuncConfig.ChecksumAlgorithm.Checksum);
    }).ToList();
    return Task.FromResult(results);
  }

}
