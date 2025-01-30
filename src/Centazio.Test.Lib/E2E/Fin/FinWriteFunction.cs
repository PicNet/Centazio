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
  
  private async Task<WriteOperationResult> WriteCustomers(WriteOperationConfig config, List<CoreSystemAndPendingCreateMap> tocreate, List<CoreSystemAndPendingUpdateMap> toupdate) {
    var created = await api.CreateAccounts(ctx, tocreate.Select(e => e.SystemEntity.To<FinAccount>()).ToList());
    await api.UpdateAccounts(toupdate.Select(e => e.SystemEntity.To<FinAccount>()).ToList());
    return new SuccessWriteOperationResult(
          created.Select((sysent, idx) => tocreate[idx].SuccessCreate(sysent.SystemId)).ToList(), 
          toupdate.Select(e => e.SuccessUpdate()).ToList());
  }
  
  private async Task<WriteOperationResult> WriteInvoices(WriteOperationConfig config, List<CoreSystemAndPendingCreateMap> tocreate, List<CoreSystemAndPendingUpdateMap> toupdate) {
    var created = await api.CreateInvoices(ctx, tocreate.Select(e => e.SystemEntity.To<FinInvoice>()).ToList());
    await api.UpdateInvoices(toupdate.Select(e => e.SystemEntity.To<FinInvoice>()).ToList());
    return new SuccessWriteOperationResult(
          created.Select((sysent, idx) => tocreate[idx].SuccessCreate(sysent.SystemId)).ToList(), 
          toupdate.Select(e => e.SuccessUpdate()).ToList());
  }
}