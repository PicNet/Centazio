using Centazio.Core;
using Centazio.Core.Runner;
using Centazio.Core.Write;

namespace Centazio.Test.Lib.E2E.Crm;

public class CrmWriteFunction(SimulationCtx ctx, CrmApi api) : WriteFunction(SimulationConstants.CRM_SYSTEM, ctx.CoreStore, ctx.CtlRepo) {

  protected override FunctionConfig<WriteOperationConfig> GetFunctionConfiguration() => new([
    new(CoreEntityTypeName.From<CoreCustomer>(), TestingDefaults.CRON_EVERY_SECOND, CovertCoreCustomerToCrm, WriteCustomers),
    new(CoreEntityTypeName.From<CoreInvoice>(), TestingDefaults.CRON_EVERY_SECOND, CovertCoreInvoiceToCrm, WriteInvoices)
  ]);
  
  private Task<CovertCoreEntitiesToSystemEntitiesResult> CovertCoreCustomerToCrm(ConvertCoreEntitiesToSystemEntitiesArgs args) {
    return Task.FromResult(WriteHelpers.CovertCoreEntitiesToSystemEntitties<CoreCustomer>(args.ToCreate, args.ToUpdate, ctx.ChecksumAlg, (id, e) => ctx.Converter.CoreCustomerToCrmCustomer(Id(id), e)));
  }
  
  private async Task<CovertCoreEntitiesToSystemEntitiesResult> CovertCoreInvoiceToCrm(ConvertCoreEntitiesToSystemEntitiesArgs args) {
    var cores = args.ToCreate.Select(e => e.CoreEntity).Concat(args.ToUpdate.Select(e => e.CoreEntity)).ToList();
    var maps = await ctx.CtlRepo.GetRelatedSystemIdsFromCores(System, CoreEntityTypeName.From<CoreCustomer>(), cores, nameof(CoreInvoice.CustomerCoreId));
    return  WriteHelpers.CovertCoreEntitiesToSystemEntitties<CoreInvoice>(args.ToCreate, args.ToUpdate, ctx.ChecksumAlg, (id, e) => ctx.Converter.CoreInvoiceToCrmInvoice(Id(id), e, maps));
  }
  
  private Guid Id(SystemEntityId systemid) => systemid == SystemEntityId.DEFAULT_VALUE ? Guid.Empty : Guid.Parse(systemid);

  private async Task<WriteOperationResult> WriteCustomers(WriteEntitiesToTargetSystemArgs args) {
    var created = await api.CreateCustomers(args.ToCreate.Select(e => e.SystemEntity.To<CrmCustomer>()).ToList());
    var updated = await api.UpdateCustomers(args.ToUpdate.Select(e => e.SystemEntity.To<CrmCustomer>()).ToList());
    return WriteHelpers.GetSuccessWriteOperationResult(args.ToCreate, created, args.ToUpdate, updated, ctx.ChecksumAlg);
  }
  
  private async Task<WriteOperationResult> WriteInvoices(WriteEntitiesToTargetSystemArgs args) {
    var created = await api.CreateInvoices(args.ToCreate.Select(e => e.SystemEntity.To<CrmInvoice>()).ToList());
    var updated = await api.UpdateInvoices(args.ToUpdate.Select(e => e.SystemEntity.To<CrmInvoice>()).ToList());
    return WriteHelpers.GetSuccessWriteOperationResult(args.ToCreate, created, args.ToUpdate, updated, ctx.ChecksumAlg);
  }
}