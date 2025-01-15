﻿using Centazio.Core;
using Centazio.Core.Misc;
using Centazio.Core.Promote;
using Centazio.Core.Runner;
using Centazio.Core.Types;

namespace Centazio.Test.Lib.E2E.Fin;

public class FinPromoteFunction(SimulationCtx ctx) : PromoteFunction(SimulationConstants.FIN_SYSTEM, ctx.StageRepository, ctx.CoreStore, ctx.CtlRepo) {

  protected override FunctionConfig<PromoteOperationConfig> GetFunctionConfiguration() => new([
    new(typeof(FinAccount), new(nameof(FinAccount)), CoreEntityTypeName.From<CoreCustomer>(), TestingDefaults.CRON_EVERY_SECOND, EvaluateCustomers) { IsBidirectional = true },
    new(typeof(FinInvoice), new(nameof(FinInvoice)), CoreEntityTypeName.From<CoreInvoice>(), TestingDefaults.CRON_EVERY_SECOND, EvaluateInvoices) { IsBidirectional = true }
  ]);
  
  Task<List<EntityEvaluationResult>> EvaluateCustomers(OperationStateAndConfig<PromoteOperationConfig> config, List<EntityForPromotionEvaluation> toeval) => Task.FromResult(toeval.Select(eval => {
    var core = ctx.Converter.FinAccountToCoreCustomer(eval.SystemEntity.To<FinAccount>(), eval.ExistingCoreEntityAndMeta?.As<CoreCustomer>());
    return eval.MarkForPromotion(eval, config.State.System, core, ctx.ChecksumAlg.Checksum);
  }).ToList());

  async Task<List<EntityEvaluationResult>> EvaluateInvoices(OperationStateAndConfig<PromoteOperationConfig> config, List<EntityForPromotionEvaluation> toeval) {
    var sysents = toeval.Select(eval => eval.SystemEntity).ToList();
    var maps = await ctx.CtlRepo.GetRelatedCoreIdsFromSystemIds(System, CoreEntityTypeName.From<CoreCustomer>(), sysents, nameof(FinInvoice.AccountId), true);
    return await toeval.Select(async eval => {
      var fininv = eval.SystemEntity.To<FinInvoice>();
      var core = await ctx.Converter.FinInvoiceToCoreInvoice(fininv, eval.ExistingCoreEntityAndMeta?.As<CoreInvoice>(), maps[fininv.AccountSystemId]);
      return eval.MarkForPromotion(eval, config.State.System, core, ctx.ChecksumAlg.Checksum);
    }).Synchronous();
  }
}