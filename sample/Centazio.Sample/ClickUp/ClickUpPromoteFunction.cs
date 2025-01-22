using Centazio.Core;
using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl;
using Centazio.Core.Misc;
using Centazio.Core.Promote;
using Centazio.Core.Runner;
using Centazio.Core.Stage;

namespace Centazio.Sample.ClickUp;

public class ClickUpPromoteFunction(IStagedEntityRepository stager, ICoreStorage corestg, ICtlRepository ctl) : PromoteFunction(Constants.Systems.ClickUp, stager, corestg, ctl) {
  
  protected override FunctionConfig<PromoteOperationConfig> GetFunctionConfiguration() => new([
    new PromoteOperationConfig(typeof(ClickUpTask), Constants.SystemEntities.ClickUp.Task, Constants.CoreEntities.Task, CronExpressionsHelper.EveryXSeconds(5), PromoteTasks) 
  ]);

  private Task<List<EntityEvaluationResult>> PromoteTasks(OperationStateAndConfig<PromoteOperationConfig> config, List<EntityForPromotionEvaluation> toeval) {
    var results  = toeval.Select(eval => {
      var cutask = eval.SystemEntity.To<ClickUpTask>();
      var task = eval.ExistingCoreEntityAndMeta?.As<CoreTask>() ?? new CoreTask(new(Guid.CreateVersion7().ToString()), cutask.name);
      // todo: users should not need to know about the checksum algorithm here
      return eval.MarkForPromotion(eval, Constants.Systems.ClickUp, task, config.FuncConfig.ChecksumAlgorithm.Checksum);
    }).ToList();
    return Task.FromResult(results);
  }

}
