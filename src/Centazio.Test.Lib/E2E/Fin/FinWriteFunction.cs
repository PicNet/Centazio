using Centazio.Core;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;
using Centazio.Core.Types;
using Centazio.Core.Write;

namespace Centazio.Test.Lib.E2E.Fin;

public class FinWriteFunction(SimulationCtx ctx, FinApi api) : WriteFunction(SimulationConstants.FIN_SYSTEM, ctx.CoreStore, ctx.CtlRepo) {

  protected override FunctionConfig<WriteOperationConfig> GetFunctionConfiguration() => new([
    new(CoreEntityTypeName.From<CoreCustomer>(), TestingDefaults.CRON_EVERY_SECOND, ConvertCoreCustomers, WriteCustomers),
    new(CoreEntityTypeName.From<CoreInvoice>(), TestingDefaults.CRON_EVERY_SECOND, ConvertCoreInvoices, WriteInvoices)
  ]);

  private Task<CovertCoreEntitiesToSystemEntitiesResult> ConvertCoreCustomers(WriteOperationConfig config, List<CoreAndPendingCreateMap> tocreate, List<CoreAndPendingUpdateMap> toupdate) {
    return Task.FromResult(CovertCoreEntitiesToSystemEntitties<CoreCustomer>(tocreate, toupdate, (id, e) => ctx.Converter.CoreCustomerToFinAccount(id, e)));
  }
  
  private async Task<CovertCoreEntitiesToSystemEntitiesResult> ConvertCoreInvoices(WriteOperationConfig config, List<CoreAndPendingCreateMap> tocreate, List<CoreAndPendingUpdateMap> toupdate) {
    var cores = tocreate.Select(e => e.CoreEntity).Concat(toupdate.Select(e => e.CoreEntity)).ToList();
    var maps = await ctx.CtlRepo.GetRelatedSystemIdsFromCores(System, CoreEntityTypeName.From<CoreCustomer>(), cores, nameof(CoreInvoice.CustomerCoreId));
    return CovertCoreEntitiesToSystemEntitties<CoreInvoice>(tocreate, toupdate, (id, e) => ctx.Converter.CoreInvoiceToFinInvoice(id, e, maps));
  }
  
  private Task<WriteOperationResult> WriteCustomers(WriteOperationConfig config, List<CoreSystemAndPendingCreateMap> tocreate, List<CoreSystemAndPendingUpdateMap> toupdate) =>
    WriteOperationResult.Create<CoreCustomer, FinAccount>(tocreate, toupdate, CreateAccounts, UpdateAccounts);

  private async Task<List<Map.Created>> CreateAccounts(List<CoreSystemAndPendingCreateMap<CoreCustomer, FinAccount>> tocreate) {
    var created = await api.CreateAccounts(ctx, tocreate.Select(e => e.SystemEntity).ToList());
    return created.Select((sysent, idx) => tocreate[idx].SuccessCreate(sysent.SystemId)).ToList(); 
  }

  private async Task<List<Map.Updated>> UpdateAccounts(List<CoreSystemAndPendingUpdateMap<CoreCustomer, FinAccount>> toupdate) {
    await api.UpdateAccounts(toupdate.Select(e => e.SystemEntity).ToList());
    return toupdate.Select(e => e.SuccessUpdate()).ToList();
  }

  private Task<WriteOperationResult> WriteInvoices(WriteOperationConfig config, List<CoreSystemAndPendingCreateMap> tocreate, List<CoreSystemAndPendingUpdateMap> toupdate) =>
      WriteOperationResult.Create<CoreInvoice, FinInvoice>(tocreate, toupdate, CreateInvoices, UpdateInvoices);

  private async Task<List<Map.Created>> CreateInvoices(List<CoreSystemAndPendingCreateMap<CoreInvoice, FinInvoice>> tocreate) {
    var created = await api.CreateInvoices(ctx, tocreate.Select(e => e.SystemEntity).ToList());
    return created.Select((sysent, idx) => tocreate[idx].SuccessCreate(sysent.SystemId)).ToList();
  }

  private async Task<List<Map.Updated>> UpdateInvoices(List<CoreSystemAndPendingUpdateMap<CoreInvoice, FinInvoice>> toupdate) {
    await api.UpdateInvoices(toupdate.Select(e => e.SystemEntity).ToList());
    return toupdate.Select(e => e.SuccessUpdate()).ToList();
  }
}