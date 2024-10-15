using Centazio.Core;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;
using Centazio.Core.Write;
using Centazio.E2E.Tests.Infra;
using Centazio.Test.Lib;

namespace Centazio.E2E.Tests.Systems.Crm;

public class CrmWriteFunction : AbstractFunction<WriteOperationConfig, WriteOperationResult>, ITargetSystemWriter {
  
  public override FunctionConfig<WriteOperationConfig> Config { get; }
  
  private readonly SimulationCtx ctx;
  private readonly CrmApi api;
  private readonly FunctionHelpers help; 

  public CrmWriteFunction(SimulationCtx ctx, CrmApi api) {
    this.ctx = ctx;
    this.api = api;
    help = new(SimulationConstants.CRM_SYSTEM, ctx.ChecksumAlg, ctx.EntityMap);
    Config = new(SimulationConstants.CRM_SYSTEM, LifecycleStage.Defaults.Write, [
      new(CoreEntityTypeName.From<CoreCustomer>(), TestingDefaults.CRON_EVERY_SECOND, this),
      new(CoreEntityTypeName.From<CoreInvoice>(), TestingDefaults.CRON_EVERY_SECOND, this)
    ]);
  }

  public async Task<(List<CoreSystemAndPendingCreateMap>, List<CoreSystemAndPendingUpdateMap>)> CovertCoreEntitiesToSystemEntitties(WriteOperationConfig config, List<CoreAndPendingCreateMap> tocreate, List<CoreAndPendingUpdateMap> toupdate) {
    if (config.Object.Value == nameof(CoreCustomer)) {
      return help.CovertCoreEntitiesToSystemEntitties<CoreCustomer>(tocreate, toupdate, (id, e) => ctx.Converter.CoreCustomerToCrmCustomer(Id(id), e));
    }
    if (config.Object.Value == nameof(CoreInvoice)) {
      var cores = tocreate.Select(e => e.CoreEntity).Concat(toupdate.Select(e => e.CoreEntity)).ToList();
      var maps = await help.GetRelatedEntitySystemIdsFromCoreIds(CoreEntityTypeName.From<CoreCustomer>(), cores, nameof(CoreInvoice.CustomerCoreId));
      return help.CovertCoreEntitiesToSystemEntitties<CoreInvoice>(tocreate, toupdate, (id, e) => ctx.Converter.CoreInvoiceToCrmInvoice(Id(id), e, maps));
    }
    throw new NotSupportedException(config.Object);
    
    Guid Id(string id) => id == String.Empty ? Guid.Empty : Guid.Parse(id);
  }

  public async Task<WriteOperationResult> WriteEntitiesToTargetSystem(WriteOperationConfig config, List<CoreSystemAndPendingCreateMap> tocreate, List<CoreSystemAndPendingUpdateMap> toupdate) {
    ctx.Debug($"CrmWriteFunction.WriteEntitiesToTargetSystem[{config.Object.Value}] Created[{tocreate.Count}] Updated[{toupdate.Count}]");
    if (config.Object.Value == nameof(CoreCustomer)) {
      var created = await api.CreateCustomers(tocreate.Select(e => e.SystemEntity.To<CrmCustomer>()).ToList());
      var updated = await api.UpdateCustomers(toupdate.Select(e => e.SystemEntity.To<CrmCustomer>()).ToList());
      return help.GetSuccessWriteOperationResult(tocreate, created, toupdate, updated);
    }
    if (config.Object.Value == nameof(CoreInvoice)) {
      var created = await api.CreateInvoices(tocreate.Select(e => e.SystemEntity.To<CrmInvoice>()).ToList());
      var updated = await api.UpdateInvoices(toupdate.Select(e => e.SystemEntity.To<CrmInvoice>()).ToList());
      return help.GetSuccessWriteOperationResult(tocreate, created, toupdate, updated);
    }
    throw new NotSupportedException(config.Object);
    
    
  }
}