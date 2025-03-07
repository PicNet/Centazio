using Centazio.Core;
using Centazio.Core.Misc;
using Centazio.Core.Promote;
using Centazio.Core.Runner;

namespace Centazio.Test.Lib.E2E.Crm;

public class CrmPromoteFunction(SimulationCtx ctx) : PromoteFunction(SimulationConstants.CRM_SYSTEM, ctx.StageRepository, ctx.CoreStore, ctx.CtlRepo, ctx.Settings) {

  public override FunctionConfig<PromoteOperationConfig> GetFunctionConfiguration() => new([
    new(typeof(CrmMembershipType), new(nameof(CrmMembershipType)), CoreEntityTypeName.From<CoreMembershipType>(), TestingDefaults.CRON_EVERY_SECOND, BuildMembershipTypes),
    new(typeof(CrmCustomer), new(nameof(CrmCustomer)), CoreEntityTypeName.From<CoreCustomer>(), TestingDefaults.CRON_EVERY_SECOND, BuildCustomers) { IsBidirectional = true },
    new(typeof(CrmInvoice), new(nameof(CrmInvoice)), CoreEntityTypeName.From<CoreInvoice>(), TestingDefaults.CRON_EVERY_SECOND, BuildInvoices) { IsBidirectional = true }
  ]);

  private Task<List<EntityEvaluationResult>> BuildMembershipTypes(OperationStateAndConfig<PromoteOperationConfig> config, List<EntityForPromotionEvaluation> toeval) => Task.FromResult(toeval.Select(eval => {
    var core = ctx.Converter.CrmMembershipTypeToCoreMembershipType(eval.SystemEntity.To<CrmMembershipType>(), eval.ExistingCoreEntityAndMeta?.As<CoreMembershipType>());
    return eval.MarkForPromotion(core);
  }).ToList());

  private Task<List<EntityEvaluationResult>> BuildCustomers(OperationStateAndConfig<PromoteOperationConfig> config, List<EntityForPromotionEvaluation> toeval) => Task.FromResult(toeval.Select(eval => {
    var core = ctx.Converter.CrmCustomerToCoreCustomer(eval.SystemEntity.To<CrmCustomer>(), eval.ExistingCoreEntityAndMeta?.As<CoreCustomer>());
    return eval.MarkForPromotion(core);
  }).ToList());

  private async Task<List<EntityEvaluationResult>> BuildInvoices(OperationStateAndConfig<PromoteOperationConfig> config, List<EntityForPromotionEvaluation> toeval) {
    var sysents = toeval.Select(eval => eval.SystemEntity).ToList();
    var maps = await ctx.CtlRepo.GetRelatedCoreIdsFromSystemIds(System, CoreEntityTypeName.From<CoreCustomer>(), sysents, nameof(CrmInvoice.CustomerId), true);
    return await toeval.Select(async eval => {
      var crminv = eval.SystemEntity.To<CrmInvoice>();
      var core = await ctx.Converter.CrmInvoiceToCoreInvoice(crminv, eval.ExistingCoreEntityAndMeta?.As<CoreInvoice>(), maps[crminv.CustomerSystemId]);
      return eval.MarkForPromotion(core);
    }).Synchronous();
  }
}