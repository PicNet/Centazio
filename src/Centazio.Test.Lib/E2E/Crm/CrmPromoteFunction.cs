using Centazio.Core;
using Centazio.Core.CoreRepo;
using Centazio.Core.Promote;
using Centazio.Core.Read;
using Centazio.Core.Runner;

namespace Centazio.Test.Lib.E2E.Crm;

public class CrmPromoteFunction : PromoteFunction {

  protected override FunctionConfig<PromoteOperationConfig> Config { get; }
  
  private readonly SimulationCtx ctx;

  public CrmPromoteFunction(SimulationCtx ctx) : base(ctx.StageRepository, ctx.CoreStore, ctx.CtlRepo){
    this.ctx = ctx;
    Config = new(SimulationConstants.CRM_SYSTEM, LifecycleStage.Defaults.Promote, [
      new(typeof(CrmMembershipType), new(nameof(CrmMembershipType)), CoreEntityTypeName.From<CoreMembershipType>(), TestingDefaults.CRON_EVERY_SECOND, this),
      new(typeof(CrmCustomer), new(nameof(CrmCustomer)), CoreEntityTypeName.From<CoreCustomer>(), TestingDefaults.CRON_EVERY_SECOND, this) { IsBidirectional = true },
      new(typeof(CrmInvoice), new(nameof(CrmInvoice)), CoreEntityTypeName.From<CoreInvoice>(), TestingDefaults.CRON_EVERY_SECOND, this) { IsBidirectional = true }
    ]);
  }
  
  public override async Task<List<EntityEvaluationResult>> BuildCoreEntities(OperationStateAndConfig<PromoteOperationConfig> config, List<EntityForPromotionEvaluation> toeval) {
    return config.State.Object.Value switch { 
      nameof(CoreMembershipType) => BuildMembershipTypes(), 
      nameof(CoreCustomer) => BuildCustomers(), 
      nameof(CoreInvoice) => await BuildInvoices(), 
      _ => throw new NotSupportedException(config.State.Object) };

    List<EntityEvaluationResult> BuildMembershipTypes() => toeval.Select(eval => {
      var core = ctx.Converter.CrmMembershipTypeToCoreMembershipType(eval.SystemEntity.To<CrmMembershipType>(), eval.ExistingCoreEntityAndMeta?.As<CoreMembershipType>());
      return MarkForPromotion(eval, core);
    }).ToList();

    List<EntityEvaluationResult> BuildCustomers() => toeval.Select(eval => {
      var core = ctx.Converter.CrmCustomerToCoreCustomer(eval.SystemEntity.To<CrmCustomer>(), eval.ExistingCoreEntityAndMeta?.As<CoreCustomer>());
      return MarkForPromotion(eval, core);
    }).ToList();

    async Task<List<EntityEvaluationResult>> BuildInvoices() {
      var sysents = toeval.Select(eval => eval.SystemEntity).ToList();
      var maps = await ctx.CtlRepo.GetRelatedCoreIdsFromSystemIds(Config.System, CoreEntityTypeName.From<CoreCustomer>(), sysents, nameof(CrmInvoice.CustomerId), true);
      return await toeval.Select(async eval => {
        var crminv = eval.SystemEntity.To<CrmInvoice>();
        var core = await ctx.Converter.CrmInvoiceToCoreInvoice(crminv, eval.ExistingCoreEntityAndMeta?.As<CoreInvoice>(), maps[crminv.CustomerSystemId]);
        return MarkForPromotion(eval, core);
      }).Synchronous();
    }
    
    EntityEvaluationResult MarkForPromotion(EntityForPromotionEvaluation eval, ICoreEntity core) => eval.MarkForPromotion(eval, SimulationConstants.CRM_SYSTEM, core, ctx.ChecksumAlg.Checksum);
  }
}