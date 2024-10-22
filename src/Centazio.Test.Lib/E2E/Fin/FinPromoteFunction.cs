using Centazio.Core;
using Centazio.Core.Promote;
using Centazio.Core.Runner;

namespace Centazio.Test.Lib.E2E.Fin;

public class FinPromoteFunction : AbstractFunction<PromoteOperationConfig, PromoteOperationResult>, IEvaluateEntitiesToPromote {
  
  public override FunctionConfig<PromoteOperationConfig> Config { get; }
  
  private readonly SimulationCtx ctx;

  public FinPromoteFunction(SimulationCtx ctx) {
    this.ctx = ctx;
    Config = new(nameof(FinApi), LifecycleStage.Defaults.Promote, [
      new(typeof(FinAccount), new(nameof(FinAccount)), CoreEntityTypeName.From<CoreCustomer>(), TestingDefaults.CRON_EVERY_SECOND, this) { IsBidirectional = true },
      new(typeof(FinInvoice), new(nameof(FinInvoice)), CoreEntityTypeName.From<CoreInvoice>(), TestingDefaults.CRON_EVERY_SECOND, this) { IsBidirectional = true }
    ]);
  }
  
  public async Task<List<EntityEvaluationResult>> BuildCoreEntities(OperationStateAndConfig<PromoteOperationConfig> config, List<EntityForPromotionEvaluation> toeval) {
    return config.State.Object.Value switch { 
      nameof(CoreCustomer) => await EvaluateCustomers(), 
      nameof(CoreInvoice) => await EvaluateInvoices(), 
      _ => throw new NotSupportedException(config.State.Object) };

    Task<List<EntityEvaluationResult>> EvaluateCustomers() => Task.FromResult(toeval.Select(eval => 
        eval.MarkForPromotion(ctx.Converter.FinAccountToCoreCustomer(eval.SystemEntity.To<FinAccount>(), eval.ExistingCoreEntity?.To<CoreCustomer>()))).ToList());

    async Task<List<EntityEvaluationResult>> EvaluateInvoices() {
      var sysents = toeval.Select(eval => eval.SystemEntity).ToList();
      var maps = await ctx.CtlRepo.GetRelatedCoreIdsFromSystemIds(Config.System, CoreEntityTypeName.From<CoreCustomer>(), sysents, nameof(FinInvoice.AccountId), true);
      return await toeval.Select(async eval => {
        var fininv = eval.SystemEntity.To<FinInvoice>();
        return eval.MarkForPromotion(await ctx.Converter.FinInvoiceToCoreInvoice(fininv, eval.ExistingCoreEntity?.To<CoreInvoice>(), maps[fininv.AccountSystemId]));
      }).Synchronous();
    }
  }
}