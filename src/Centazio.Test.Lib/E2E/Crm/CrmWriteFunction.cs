using Centazio.Core;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;
using Centazio.Core.Types;
using Centazio.Core.Write;

namespace Centazio.Test.Lib.E2E.Crm;

public class CrmWriteFunction(SimulationCtx ctx, CrmApi api) : WriteFunction(SimulationConstants.CRM_SYSTEM, ctx.CoreStore, ctx.CtlRepo) {

  protected override FunctionConfig<WriteOperationConfig> GetFunctionConfiguration() => new([
    new(CoreEntityTypeName.From<CoreCustomer>(), TestingDefaults.CRON_EVERY_SECOND, CovertCoreCustomerToCrm, WriteCustomers),
    new(CoreEntityTypeName.From<CoreInvoice>(), TestingDefaults.CRON_EVERY_SECOND, CovertCoreInvoiceToCrm, WriteInvoices)
  ]);
  
  private Task<CovertCoreEntitiesToSystemEntitiesResult> CovertCoreCustomerToCrm(WriteOperationConfig config, List<CoreAndPendingCreateMap> tocreate, List<CoreAndPendingUpdateMap> toupdate) {
    return Task.FromResult(WriteHelpers.CovertCoreEntitiesToSystemEntitties<CoreCustomer>(tocreate, toupdate, ctx.ChecksumAlg, (id, e) => ctx.Converter.CoreCustomerToCrmCustomer(Id(id), e)));
  }
  
  private async Task<CovertCoreEntitiesToSystemEntitiesResult> CovertCoreInvoiceToCrm(WriteOperationConfig config, List<CoreAndPendingCreateMap> tocreate, List<CoreAndPendingUpdateMap> toupdate) {
    var cores = tocreate.Select(e => e.CoreEntity).Concat(toupdate.Select(e => e.CoreEntity)).ToList();
    var maps = await ctx.CtlRepo.GetRelatedSystemIdsFromCores(System, CoreEntityTypeName.From<CoreCustomer>(), cores, nameof(CoreInvoice.CustomerCoreId));
    return  WriteHelpers.CovertCoreEntitiesToSystemEntitties<CoreInvoice>(tocreate, toupdate, ctx.ChecksumAlg, (id, e) => ctx.Converter.CoreInvoiceToCrmInvoice(Id(id), e, maps));
  }
  
  private Guid Id(SystemEntityId systemid) => systemid == SystemEntityId.DEFAULT_VALUE ? Guid.Empty : Guid.Parse(systemid);

  private async Task<WriteOperationResult> WriteCustomers(WriteOperationConfig config, List<CoreSystemAndPendingCreateMap> tocreate, List<CoreSystemAndPendingUpdateMap> toupdate) {
    var created = await api.CreateCustomers(tocreate.Select(e => e.SystemEntity.To<CrmCustomer>()).ToList());
    var updated = await api.UpdateCustomers(toupdate.Select(e => e.SystemEntity.To<CrmCustomer>()).ToList());
    return WriteHelpers.GetSuccessWriteOperationResult(tocreate, created, toupdate, updated, ctx.ChecksumAlg);
  }
  
  private async Task<WriteOperationResult> WriteInvoices(WriteOperationConfig config, List<CoreSystemAndPendingCreateMap> tocreate, List<CoreSystemAndPendingUpdateMap> toupdate) {
    var created = await api.CreateInvoices(tocreate.Select(e => e.SystemEntity.To<CrmInvoice>()).ToList());
    var updated = await api.UpdateInvoices(toupdate.Select(e => e.SystemEntity.To<CrmInvoice>()).ToList());
    return WriteHelpers.GetSuccessWriteOperationResult(tocreate, created, toupdate, updated, ctx.ChecksumAlg);
  }
}