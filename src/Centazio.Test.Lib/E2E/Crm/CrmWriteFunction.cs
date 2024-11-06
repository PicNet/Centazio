using Centazio.Core;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Core.Write;

namespace Centazio.Test.Lib.E2E.Crm;

public class CrmWriteFunction : WriteFunction {

  protected override FunctionConfig<WriteOperationConfig> Config { get; }
  
  private readonly SimulationCtx ctx;
  private readonly CrmApi api;

  public CrmWriteFunction(SimulationCtx ctx, CrmApi api) : base(ctx.CoreStore, ctx.CtlRepo) {
    this.ctx = ctx;
    this.api = api;
    Config = new(SimulationConstants.CRM_SYSTEM, LifecycleStage.Defaults.Write, [
      new(CoreEntityTypeName.From<CoreCustomer>(), TestingDefaults.CRON_EVERY_SECOND, this),
      new(CoreEntityTypeName.From<CoreInvoice>(), TestingDefaults.CRON_EVERY_SECOND, this)
    ]);
  }

  public override async Task<CovertCoreEntitiesToSystemEntitiesResult> CovertCoreEntitiesToSystemEntities(WriteOperationConfig config, List<CoreAndPendingCreateMap> tocreate, List<CoreAndPendingUpdateMap> toupdate) {
    if (config.Object.Value == nameof(CoreCustomer)) {
      return WriteHelpers.CovertCoreEntitiesToSystemEntitties<CoreCustomer>(tocreate, toupdate, ctx.ChecksumAlg, (id, e) => ctx.Converter.CoreCustomerToCrmCustomer(Id(id), e));
    }
    if (config.Object.Value == nameof(CoreInvoice)) {
      var cores = tocreate.Select(e => e.CoreEntity).Concat(toupdate.Select(e => e.CoreEntity)).ToList();
      var maps = await ctx.CtlRepo.GetRelatedSystemIdsFromCores(Config.System, CoreEntityTypeName.From<CoreCustomer>(), cores, nameof(CoreInvoice.CustomerCoreId));
      return WriteHelpers.CovertCoreEntitiesToSystemEntitties<CoreInvoice>(tocreate, toupdate, ctx.ChecksumAlg, (id, e) => ctx.Converter.CoreInvoiceToCrmInvoice(Id(id), e, maps));
    }
    throw new NotSupportedException(config.Object);
    
    Guid Id(SystemEntityId id) => id == SystemEntityId.DEFAULT_VALUE ? Guid.Empty : Guid.Parse(id);
  }

  public override async Task<WriteOperationResult> WriteEntitiesToTargetSystem(
      WriteOperationConfig config, 
      List<CoreSystemAndPendingCreateMap> tocreate, 
      List<CoreSystemAndPendingUpdateMap> toupdate) {
    ctx.Debug($"CrmWriteFunction.WriteEntitiesToTargetSystem[{config.Object.Value}] Created[{tocreate.Count}] Updated[{toupdate.Count}]");
    if (config.Object.Value == nameof(CoreCustomer)) {
      var created = await api.CreateCustomers(tocreate.Select(e => e.SystemEntity.To<CrmCustomer>()).ToList());
      var updated = await api.UpdateCustomers(toupdate.Select(e => e.SystemEntity.To<CrmCustomer>()).ToList());
      return WriteHelpers.GetSuccessWriteOperationResult(tocreate, created, toupdate, updated, ctx.ChecksumAlg);
    }
    if (config.Object.Value == nameof(CoreInvoice)) {
      var created = await api.CreateInvoices(tocreate.Select(e => e.SystemEntity.To<CrmInvoice>()).ToList());
      var updated = await api.UpdateInvoices(toupdate.Select(e => e.SystemEntity.To<CrmInvoice>()).ToList());
      return WriteHelpers.GetSuccessWriteOperationResult(tocreate, created, toupdate, updated, ctx.ChecksumAlg);
    }
    throw new NotSupportedException(config.Object);
    
    
  }
}