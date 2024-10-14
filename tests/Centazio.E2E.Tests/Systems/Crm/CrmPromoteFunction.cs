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
      new(typeof(CrmMembershipType), new(nameof(CrmMembershipType)), CoreEntityType.From<CoreMembershipType>(), TestingDefaults.CRON_EVERY_SECOND, this),
      new(typeof(CrmCustomer), new(nameof(CrmCustomer)), CoreEntityType.From<CoreCustomer>(), TestingDefaults.CRON_EVERY_SECOND, this) { IsBidirectional = true },
      new(typeof(CrmInvoice), new(nameof(CrmInvoice)), CoreEntityType.From<CoreInvoice>(), TestingDefaults.CRON_EVERY_SECOND, this) { IsBidirectional = true }
    ]);
  }
  
  public async Task<PromoteOperationResult> BuildCoreEntities(OperationStateAndConfig<PromoteOperationConfig> config, List<Containers.StagedSysOptionalCore> staged) {
    var topromote = config.State.Object.Value switch { 
      nameof(CoreMembershipType) => BuildMembershipTypes(), 
      nameof(CoreCustomer) => BuildCustomers(), 
      nameof(CoreInvoice) => await BuildInvoices(), 
      _ => throw new NotSupportedException(config.State.Object) };
    return new SuccessPromoteOperationResult(topromote, []);

    List<Containers.StagedSysCore> BuildMembershipTypes() => staged.Select(t => 
        t.SetCore(ctx.Converter.CrmMembershipTypeToCoreMembershipType(t.Sys.To<CrmMembershipType>(), t.OptCore?.To<CoreMembershipType>()))).ToList();

    List<Containers.StagedSysCore> BuildCustomers() => staged.Select(t => 
        t.SetCore(ctx.Converter.CrmCustomerToCoreCustomer(t.Sys.To<CrmCustomer>(), t.OptCore?.To<CoreCustomer>()))).ToList();

    async Task<List<Containers.StagedSysCore>> BuildInvoices() {
      var maps = await help.GetRelatedEntityCoreIdsFromSystemIds(CoreEntityType.From<CoreCustomer>(), staged, nameof(CrmInvoice.CustomerId), true);
      return staged.Select(t => {
        var crminv = t.Sys.To<CrmInvoice>();
        return t.SetCore(ctx.Converter.CrmInvoiceToCoreInvoice(crminv, t.OptCore?.To<CoreInvoice>(), maps[crminv.CustomerSystemId]));
      }).ToList();
    }
  }

}