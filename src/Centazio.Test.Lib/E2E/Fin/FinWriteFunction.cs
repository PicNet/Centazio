using Centazio.Core;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;
using Centazio.Core.Write;

namespace Centazio.Test.Lib.E2E.Fin;

public class FinWriteFunction(SimulationCtx ctx, FinApi api) : WriteFunction(SimulationConstants.FIN_SYSTEM, ctx.CoreStore, ctx.CtlRepo) {

  protected override FunctionConfig<WriteOperationConfig> GetFunctionConfiguration() => new([
    new(CoreEntityTypeName.From<CoreCustomer>(), TestingDefaults.CRON_EVERY_SECOND, ConvertCoreCustomers, WriteCustomers),
    new(CoreEntityTypeName.From<CoreInvoice>(), TestingDefaults.CRON_EVERY_SECOND, ConvertCoreInvoices, WriteInvoices)
  ]);

  private Task<CovertCoreEntitiesToSystemEntitiesResult> ConvertCoreCustomers(WriteOperationConfig config, List<CoreAndPendingCreateMap> tocreate, List<CoreAndPendingUpdateMap> toupdate) {
    return Task.FromResult(WriteHelpers.CovertCoreEntitiesToSystemEntitties<CoreCustomer>(tocreate, toupdate, ctx.ChecksumAlg, (id, e) => ctx.Converter.CoreCustomerToFinAccount(Id(id), e)));
  }
  
  private async Task<CovertCoreEntitiesToSystemEntitiesResult> ConvertCoreInvoices(WriteOperationConfig config, List<CoreAndPendingCreateMap> tocreate, List<CoreAndPendingUpdateMap> toupdate) {
    var cores = tocreate.Select(e => e.CoreEntity).Concat(toupdate.Select(e => e.CoreEntity)).ToList();
    var maps = await ctx.CtlRepo.GetRelatedSystemIdsFromCores(System, CoreEntityTypeName.From<CoreCustomer>(), cores, nameof(CoreInvoice.CustomerCoreId));
    return WriteHelpers.CovertCoreEntitiesToSystemEntitties<CoreInvoice>(tocreate, toupdate, ctx.ChecksumAlg, (id, e) => ctx.Converter.CoreInvoiceToFinInvoice(Id(id), e, maps));
  }
  
  private int Id(SystemEntityId systemid) => systemid == SystemEntityId.DEFAULT_VALUE ? 0 : Int32.Parse(systemid);

  private async Task<WriteOperationResult> WriteCustomers(WriteOperationConfig config, List<CoreSystemAndPendingCreateMap> tocreate, List<CoreSystemAndPendingUpdateMap> toupdate) {
    var created = await api.CreateAccounts(tocreate.Select(e => e.SystemEntity.To<FinAccount>()).ToList());
    var updated = await api.UpdateAccounts(toupdate.Select(e => e.SystemEntity.To<FinAccount>()).ToList());
    return WriteHelpers.GetSuccessWriteOperationResult(tocreate, created, toupdate, updated, ctx.ChecksumAlg);
  }
  
  private async Task<WriteOperationResult> WriteInvoices(WriteOperationConfig config, List<CoreSystemAndPendingCreateMap> tocreate, List<CoreSystemAndPendingUpdateMap> toupdate) {
    var created = await api.CreateInvoices(tocreate.Select(e => e.SystemEntity.To<FinInvoice>()).ToList());
    var updated = await api.UpdateInvoices(toupdate.Select(e => e.SystemEntity.To<FinInvoice>()).ToList());
    return WriteHelpers.GetSuccessWriteOperationResult(tocreate, created, toupdate, updated, ctx.ChecksumAlg);
  }
}