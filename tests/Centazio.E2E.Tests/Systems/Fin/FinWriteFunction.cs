using Centazio.Core;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Runner;
using Centazio.Core.Write;
using Centazio.E2E.Tests.Infra;
using Centazio.Test.Lib;

namespace Centazio.E2E.Tests.Systems.Fin;

public class FinWriteFunction : AbstractFunction<WriteOperationConfig, WriteOperationResult>, ITargetSystemWriter {
  
  public override FunctionConfig<WriteOperationConfig> Config { get; }
  
  private readonly SimulationCtx ctx;
  private readonly FinApi api;
  private readonly FunctionHelpers help;

  public FinWriteFunction(SimulationCtx ctx, FinApi api) {
    this.ctx = ctx;
    this.api = api;
    help = new(SimulationConstants.FIN_SYSTEM, ctx.ChecksumAlg, ctx.EntityMap);
    Config = new(SimulationConstants.FIN_SYSTEM, LifecycleStage.Defaults.Write, [
      new(CoreEntityType.From<CoreCustomer>(), TestingDefaults.CRON_EVERY_SECOND, this),
      new(CoreEntityType.From<CoreInvoice>(), TestingDefaults.CRON_EVERY_SECOND, this)
    ]);
  }

  public async Task<(List<CoreSystemAndPendingCreateMap>, List<CoreSystemAndPendingUpdateMap>)> CovertCoreEntitiesToSystemEntitties(WriteOperationConfig config, List<CoreAndPendingCreateMap> tocreate, List<CoreAndPendingUpdateMap> toupdate) {
    ctx.Debug($"FinWriteFunction.CovertCoreEntitiesToSystemEntitties[{config.Object.Value}] ToCreate[{tocreate.Count}] ToUpdate[{toupdate.Count}]");
    if (config.Object.Value == nameof(CoreCustomer)) {
      return help.CovertCoreEntitiesToSystemEntitties<CoreCustomer>(tocreate, toupdate, (id, e) => ctx.Converter.CoreCustomerToFinAccount(Id(id), e));
    }
    if (config.Object.Value == nameof(CoreInvoice)) {
      var cores = tocreate.ToCore().Concat(toupdate.ToCore()).ToList();
      var maps = await help.GetRelatedEntitySystemIdsFromCoreIds(CoreEntityType.From<CoreCustomer>(), cores, nameof(CoreInvoice.CustomerCoreId));
      return help.CovertCoreEntitiesToSystemEntitties<CoreInvoice>(tocreate, toupdate, (id, e) => ctx.Converter.CoreInvoiceToFinInvoice(Id(id), e, maps));
    }
    throw new NotSupportedException(config.Object);
    
    int Id(string id) => id == String.Empty ? 0 : Int32.Parse(id);
  }

  public async Task<WriteOperationResult> WriteEntitiesToTargetSystem(WriteOperationConfig config, List<CoreSystemAndPendingCreateMap> tocreate, List<CoreSystemAndPendingUpdateMap> toupdate) {
    ctx.Debug($"FinWriteFunction.WriteEntitiesToTargetSystem[{config.Object.Value}] Created[{tocreate.Count}] Updated[{toupdate.Count}]");
    if (config.Object.Value == nameof(CoreCustomer)) {
      var (created, updated) = (await api.CreateAccounts(tocreate.ToSysEnt<FinAccount>()), await api.UpdateAccounts(toupdate.ToSysEnt<FinAccount>()));
      return new SuccessWriteOperationResult(tocreate.SuccessCreate(created, ctx.ChecksumAlg), toupdate.SuccessUpdate(updated, ctx.ChecksumAlg));
    }
    if (config.Object.Value == nameof(CoreInvoice)) {
      var (created, updated) = (await api.CreateInvoices(tocreate.ToSysEnt<FinInvoice>()), await api.UpdateInvoices(toupdate.ToSysEnt<FinInvoice>()));
      return new SuccessWriteOperationResult(tocreate.SuccessCreate(created, ctx.ChecksumAlg), toupdate.SuccessUpdate(updated, ctx.ChecksumAlg));
    }
    throw new NotSupportedException(config.Object);
  }
}