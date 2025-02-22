using Centazio.Core;
using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl;
using Centazio.Core.Misc;
using Centazio.Core.Promote;
using Centazio.Core.Runner;
using Centazio.Core.Settings;
using Centazio.Core.Stage;

namespace Centazio.Sample.ClickUp;

public class ClickUpPromoteFunction(IStagedEntityRepository stager, ICoreStorage corestg, ICtlRepository ctl, CentazioSettings settings) : PromoteFunction(SampleConstants.Systems.ClickUp, stager, corestg, ctl, settings) {
  
  protected override FunctionConfig<PromoteOperationConfig> GetFunctionConfiguration() => new([
    new PromoteOperationConfig(typeof(ClickUpTask), SampleConstants.SystemEntities.ClickUp.Task, SampleConstants.CoreEntities.Task, CronExpressionsHelper.EveryXSeconds(5), PromoteTasks) 
  ]);

  private Task<List<EntityEvaluationResult>> PromoteTasks(OperationStateAndConfig<PromoteOperationConfig> config, List<EntityForPromotionEvaluation> toeval) {
    var results  = toeval.Select(eval => {
      var source = eval.SystemEntity.To<ClickUpTask>();
      if (eval.IsNewEntity && source.IsCompleted) return eval.MarkForIgnore(new ("New task in completed status is being ignored"));
      var target = (eval.ExistingCoreEntityAndMeta?.As<CoreTask>() ?? new CoreTask(new(Guid.CreateVersion7().ToString()), String.Empty, false)) with { Name = source.name, Completed = source.IsCompleted };
      return eval.MarkForPromotion(target);
    }).ToList();
    return Task.FromResult(results);
  }

}
