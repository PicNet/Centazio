using Centazio.Core.Runner;
using Centazio.Core.Write;

namespace Centazio.Test.Lib.E2E.Crm;

public class CrmWriteFunction(SimulationCtx ctx, CrmApi api) : WriteFunction(SimulationConstants.CRM_SYSTEM, ctx.CoreStore, ctx.CtlRepo) {

  public override FunctionConfig<WriteOperationConfig> GetFunctionConfiguration() => new([
    new(CoreEntityTypeName.From<CoreCustomer>(), TestingDefaults.CRON_EVERY_SECOND, CovertCoreCustomerToCrm, WriteCustomers),
    new(CoreEntityTypeName.From<CoreInvoice>(), TestingDefaults.CRON_EVERY_SECOND, CovertCoreInvoiceToCrm, WriteInvoices)
  ]);
  
  private Task<CovertCoresToSystemsResult> CovertCoreCustomerToCrm(WriteOperationConfig config, List<CoreAndPendingCreateMap> tocreate, List<CoreAndPendingUpdateMap> toupdate) {
    return Task.FromResult(CovertCoresToSystems<CoreCustomer>(tocreate, toupdate, (id, e) => ctx.Converter.CoreCustomerToCrmCustomer(id, e)));
  }
  
  private async Task<CovertCoresToSystemsResult> CovertCoreInvoiceToCrm(WriteOperationConfig config, List<CoreAndPendingCreateMap> tocreate, List<CoreAndPendingUpdateMap> toupdate) {
    var cores = tocreate.Select(e => e.CoreEntity).Concat(toupdate.Select(e => e.CoreEntity)).ToList();
    var maps = await ctx.CtlRepo.GetRelatedSystemIdsFromCores(System, CoreEntityTypeName.From<CoreCustomer>(), cores, nameof(CoreInvoice.CustomerCoreId));
    return  CovertCoresToSystems<CoreInvoice>(tocreate, toupdate, (id, e) => ctx.Converter.CoreInvoiceToCrmInvoice(id, e, maps));
  }
  
  private Task<WriteOperationResult> WriteCustomers(WriteOperationConfig config, List<CoreSystemAndPendingCreateMap> tocreate, List<CoreSystemAndPendingUpdateMap> toupdate) =>
      WriteOperationResult.Create<CoreCustomer, CrmCustomer>(tocreate, toupdate, CreateCustomers, UpdateCustomers);

  private async Task<List<Map.Created>> CreateCustomers(List<CoreSystemAndPendingCreateMap<CoreCustomer, CrmCustomer>> tocreate) {
    var created = await api.CreateCustomers(ctx, tocreate.Select(e => e.SystemEntity).ToList());
    return created.Select((sysent, idx) => tocreate[idx].SuccessCreate(sysent.SystemId)).ToList();
  }

  private async Task<List<Map.Updated>> UpdateCustomers(List<CoreSystemAndPendingUpdateMap<CoreCustomer, CrmCustomer>> toupdate) {
    await api.UpdateCustomers(toupdate.Select(e => e.SystemEntity).ToList());
    return toupdate.Select(e => e.SuccessUpdate()).ToList();
  }
  
  private Task<WriteOperationResult> WriteInvoices(WriteOperationConfig config, List<CoreSystemAndPendingCreateMap> tocreate, List<CoreSystemAndPendingUpdateMap> toupdate) =>
      WriteOperationResult.Create<CoreInvoice, CrmInvoice>(tocreate, toupdate, CreateInvoices, UpdateInvoices);

  private async Task<List<Map.Created>> CreateInvoices(List<CoreSystemAndPendingCreateMap<CoreInvoice, CrmInvoice>> tocreate) {
    var created = await api.CreateInvoices(ctx, tocreate.Select(e => e.SystemEntity).ToList());
    return created.Select((sysent, idx) => tocreate[idx].SuccessCreate(sysent.SystemId)).ToList();
  }

  private async Task<List<Map.Updated>> UpdateInvoices(List<CoreSystemAndPendingUpdateMap<CoreInvoice, CrmInvoice>> toupdate) {
    await api.UpdateInvoices(toupdate.Select(e => e.SystemEntity).ToList());
    return toupdate.Select(e => e.SuccessUpdate()).ToList();
  }
}