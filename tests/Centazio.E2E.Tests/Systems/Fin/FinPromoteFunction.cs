using Centazio.Core;
using Centazio.Core.Promote;
using Centazio.Core.Runner;
using Centazio.E2E.Tests.Infra;
using Centazio.Test.Lib;

namespace Centazio.E2E.Tests.Systems.Fin;

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
  
  public async Task<PromoteOperationResult> BuildCoreEntities(OperationStateAndConfig<PromoteOperationConfig> config, List<Containers.StagedSysOptionalCore> staged) {
    ctx.Debug($"FinPromoteFunction[{config.OpConfig.Object.Value}] Staged[{staged.Count}]");
    var topromote = config.State.Object.Value switch { 
      nameof(CoreCustomer) => await EvaluateCustomers(), 
      nameof(CoreInvoice) => await EvaluateInvoices(), 
      _ => throw new NotSupportedException(config.State.Object) };
    return new SuccessPromoteOperationResult(topromote, []);

    Task<List<Containers.StagedSysCore>> EvaluateCustomers() {
      return Task.FromResult(staged.Select(t => 
          t.SetCore(ctx.Converter.FinAccountToCoreCustomer(t.Sys.To<FinAccount>(), t.OptCore?.To<CoreCustomer>()))).ToList());
    }

    async Task<List<Containers.StagedSysCore>> EvaluateInvoices() {
      var maps = await ctx.FinHelpers.GetRelatedEntityCoreIdsFromSystemIds(CoreEntityTypeName.From<CoreCustomer>(), staged, nameof(FinInvoice.AccountId), true);
      return staged.Select(t => {
        var fininv = t.Sys.To<FinInvoice>();
        return t.SetCore(ctx.Converter.FinInvoiceToCoreInvoice(fininv, t.OptCore?.To<CoreInvoice>(), maps[fininv.AccountSystemId]));
      }).ToList();
    }
  }

}