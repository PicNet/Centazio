using Centazio.Core;
using Centazio.Core.Runner;
using Centazio.Core.Write;

namespace Centazio.Test.Lib.E2E.Fin;

public class FinWriteFunction(SimulationCtx ctx, FinApi api) : WriteFunction(SimulationConstants.FIN_SYSTEM, ctx.CoreStore, ctx.CtlRepo) {

  protected override FunctionConfig<WriteOperationConfig> GetFunctionConfiguration() => new([
    new(CoreEntityTypeName.From<CoreCustomer>(), TestingDefaults.CRON_EVERY_SECOND, ConvertCoreCustomers, WriteCustomers),
    new(CoreEntityTypeName.From<CoreInvoice>(), TestingDefaults.CRON_EVERY_SECOND, ConvertCoreInvoices, WriteInvoices)
  ]);

  private Task<CovertCoreEntitiesToSystemEntitiesResult> ConvertCoreCustomers(ConvertCoreEntitiesToSystemEntitiesArgs args) {
    return Task.FromResult(WriteHelpers.CovertCoreEntitiesToSystemEntitties<CoreCustomer>(args.ToCreate, args.ToUpdate, ctx.ChecksumAlg, (id, e) => ctx.Converter.CoreCustomerToFinAccount(Id(id), e)));
  }
  
  private async Task<CovertCoreEntitiesToSystemEntitiesResult> ConvertCoreInvoices(ConvertCoreEntitiesToSystemEntitiesArgs args) {
    var cores = args.ToCreate.Select(e => e.CoreEntity).Concat(args.ToUpdate.Select(e => e.CoreEntity)).ToList();
    var maps = await ctx.CtlRepo.GetRelatedSystemIdsFromCores(System, CoreEntityTypeName.From<CoreCustomer>(), cores, nameof(CoreInvoice.CustomerCoreId));
    return WriteHelpers.CovertCoreEntitiesToSystemEntitties<CoreInvoice>(args.ToCreate, args.ToUpdate, ctx.ChecksumAlg, (id, e) => ctx.Converter.CoreInvoiceToFinInvoice(Id(id), e, maps));
  }
  
  private int Id(SystemEntityId systemid) => systemid == SystemEntityId.DEFAULT_VALUE ? 0 : Int32.Parse(systemid);

  private async Task<WriteOperationResult> WriteCustomers(WriteEntitiesToTargetSystemArgs args) {
    var created = await api.CreateAccounts(args.ToCreate.Select(e => e.SystemEntity.To<FinAccount>()).ToList());
    var updated = await api.UpdateAccounts(args.ToUpdate.Select(e => e.SystemEntity.To<FinAccount>()).ToList());
    return WriteHelpers.GetSuccessWriteOperationResult(args.ToCreate, created, args.ToUpdate, updated, ctx.ChecksumAlg);
  }
  
  private async Task<WriteOperationResult> WriteInvoices(WriteEntitiesToTargetSystemArgs args) {
    var created = await api.CreateInvoices(args.ToCreate.Select(e => e.SystemEntity.To<FinInvoice>()).ToList());
    var updated = await api.UpdateInvoices(args.ToUpdate.Select(e => e.SystemEntity.To<FinInvoice>()).ToList());
    return WriteHelpers.GetSuccessWriteOperationResult(args.ToCreate, created, args.ToUpdate, updated, ctx.ChecksumAlg);
  }
}