using Centazio.Core;
using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl;
using Centazio.Core.Misc;
using Centazio.Core.Promote;
using Centazio.Core.Runner;
using Centazio.Core.Stage;

namespace Centazio.Sample.GoogleSheets;

public class GoogleSheetsPromoteFunction(IStagedEntityRepository stager, ICoreStorage corestg, ICtlRepository ctl) : PromoteFunction(SampleConstants.Systems.GoogleSheets, stager, corestg, ctl) {
  
  protected override FunctionConfig<PromoteOperationConfig> GetFunctionConfiguration() => new([
    new PromoteOperationConfig(typeof(GoogleSheetsTaskRow), SampleConstants.SystemEntities.GoogleSheets.TaskRow, SampleConstants.CoreEntities.Task, CronExpressionsHelper.EveryXSeconds(5), PromoteTasks) 
  ]);

  private Task<List<EntityEvaluationResult>> PromoteTasks(OperationStateAndConfig<PromoteOperationConfig> config, List<EntityForPromotionEvaluation> toeval) {
    var results  = toeval.Select(eval => {
      var row = eval.SystemEntity.To<GoogleSheetsTaskRow>();
      var task = eval.ExistingCoreEntityAndMeta?.As<CoreTask>() ?? new CoreTask(new(Guid.CreateVersion7().ToString()), row.Value);
      // todo: why do we have to pass `SampleConstants.Systems.GoogleSheets` here, when the function already knows the system.  The user should not have to use this twice
      return eval.MarkForPromotion(SampleConstants.Systems.GoogleSheets, task);
    }).ToList();
    return Task.FromResult(results);
  }

}
