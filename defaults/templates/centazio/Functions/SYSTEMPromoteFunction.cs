using Centazio.Core.Promote;
using Centazio.Core.Runner;
using Centazio.Core.Stage;

namespace {{ it.Namespace }};

public class {{ it.SystemName }}PromoteFunction(IStagedEntityRepository stager, ICoreStorage corestg, ICtlRepository ctl) : PromoteFunction({{ it.SystemName }}Constants.{{ it.SystemName }}SystemName, stager, corestg, ctl) {

  protected override FunctionConfig GetFunctionConfiguration() => new([
    new PromoteOperationConfig(System, typeof({{ it.SystemName }}ExampleEntity), {{ it.SystemName }}Constants.{{ it.SystemName }}ExampleEntityName, CoreEntityTypes.ExampleEntity, CronExpressionsHelper.EveryXSeconds(5), PromoteEntities) 
  ]);

  private Task<List<EntityEvaluationResult>> PromoteEntities(OperationStateAndConfig<PromoteOperationConfig> config, List<EntityForPromotionEvaluation> toeval) {
    var results  = toeval.Select(eval => {
      var source = eval.SystemEntity.To<{{ it.SystemName }}ExampleEntity>();
      var target = (eval.ExistingCoreEntityAndMeta?.As<ExampleEntity>() ?? new ExampleEntity(new(Guid.CreateVersion7().ToString()), source.CorrelationId, String.Empty, false)) with { Name = source.name };
      return eval.MarkForPromotion(target);
    }).ToList();
    return Task.FromResult(results);
  }

}
