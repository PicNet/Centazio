using Centazio.Core;
using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl;
using Centazio.Core.Misc;
using Centazio.Core.Promote;
using Centazio.Core.Runner;
using Centazio.Core.Stage;

namespace Centazio.Sample.AppSheet;

public class AppSheetPromoteFunction(IStagedEntityRepository stager, ICoreStorage corestg, ICtlRepository ctl) : PromoteFunction(SampleConstants.Systems.AppSheet, stager, corestg, ctl) {
  
  protected override FunctionConfig<PromoteOperationConfig> GetFunctionConfiguration() => new([
    new PromoteOperationConfig(typeof(AppSheetTask), SampleConstants.SystemEntities.AppSheet.TaskRow, SampleConstants.CoreEntities.Task, CronExpressionsHelper.EveryXSeconds(5), PromoteTasks) 
  ]);

  private Task<List<EntityEvaluationResult>> PromoteTasks(OperationStateAndConfig<PromoteOperationConfig> config, List<EntityForPromotionEvaluation> toeval) {
    var results  = toeval.Select(eval => {
      var astask = eval.SystemEntity.To<AppSheetTask>();
      var task = (eval.ExistingCoreEntityAndMeta?.As<CoreTask>() ?? new CoreTask(new(Guid.CreateVersion7().ToString()), String.Empty)) with { Name = astask.Task ?? throw new Exception() };
      
      // todo: why do we have to pass `SampleConstants.Systems.AppSheet` here, when the function already knows the system.  The user should not have to use this twice
      return eval.MarkForPromotion(SampleConstants.Systems.AppSheet, task);
    }).ToList();
    return Task.FromResult(results);
  }

}
