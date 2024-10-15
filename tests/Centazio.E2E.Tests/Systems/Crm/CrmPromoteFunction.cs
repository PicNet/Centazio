using Centazio.Core;
using Centazio.Core.Promote;
using Centazio.Core.Runner;
using Centazio.E2E.Tests.Infra;
using Centazio.Test.Lib;

namespace Centazio.E2E.Tests.Systems.Crm;

public class CrmPromoteFunction : AbstractFunction<PromoteOperationConfig, PromoteOperationResult>, IEvaluateEntitiesToPromote {
  
  public override FunctionConfig<PromoteOperationConfig> Config { get; }
  
  private readonly SimulationCtx ctx;
  private readonly FunctionHelpers help;

  public CrmPromoteFunction(SimulationCtx ctx) {
    this.ctx = ctx;
    help = new FunctionHelpers(SimulationConstants.CRM_SYSTEM, ctx.ChecksumAlg, ctx.EntityMap); 
    Config = new(SimulationConstants.CRM_SYSTEM, LifecycleStage.Defaults.Promote, [
      new(typeof(CrmMembershipType), new(nameof(CrmMembershipType)), CoreEntityTypeName.From<CoreMembershipType>(), TestingDefaults.CRON_EVERY_SECOND, this),
      new(typeof(CrmCustomer), new(nameof(CrmCustomer)), CoreEntityTypeName.From<CoreCustomer>(), TestingDefaults.CRON_EVERY_SECOND, this) { IsBidirectional = true },
      new(typeof(CrmInvoice), new(nameof(CrmInvoice)), CoreEntityTypeName.From<CoreInvoice>(), TestingDefaults.CRON_EVERY_SECOND, this) { IsBidirectional = true }
    ]);
  }
  
  public async Task<List<EntityEvaluationResult>> BuildCoreEntities(OperationStateAndConfig<PromoteOperationConfig> config, List<EntityForPromotionEvaluation> toeval) {
    return config.State.Object.Value switch { 
      nameof(CoreMembershipType) => BuildMembershipTypes(), 
      nameof(CoreCustomer) => BuildCustomers(), 
      nameof(CoreInvoice) => await BuildInvoices(), 
      _ => throw new NotSupportedException(config.State.Object) };

    List<EntityEvaluationResult> BuildMembershipTypes() => toeval.Select(eval => 
        eval.MarkForPromotion(ctx.Converter.CrmMembershipTypeToCoreMembershipType(eval.SystemEntity.To<CrmMembershipType>(), eval.ExistingCoreEntity?.To<CoreMembershipType>()))).ToList();

    List<EntityEvaluationResult> BuildCustomers() => toeval.Select(eval => 
        eval.MarkForPromotion(ctx.Converter.CrmCustomerToCoreCustomer(eval.SystemEntity.To<CrmCustomer>(), eval.ExistingCoreEntity?.To<CoreCustomer>()))).ToList();

    async Task<List<EntityEvaluationResult>> BuildInvoices() {
      var sysents = toeval.Select(eval => eval.SystemEntity).ToList();
      var maps = await help.GetRelatedEntityCoreIdsFromSystemIds(CoreEntityTypeName.From<CoreCustomer>(), sysents, nameof(CrmInvoice.CustomerId), true);
      return toeval.Select(eval => {
        var crminv = eval.SystemEntity.To<CrmInvoice>();
        return eval.MarkForPromotion(ctx.Converter.CrmInvoiceToCoreInvoice(crminv, eval.ExistingCoreEntity?.To<CoreInvoice>(), maps[crminv.CustomerSystemId]));
      }).ToList();
    }
  }
}