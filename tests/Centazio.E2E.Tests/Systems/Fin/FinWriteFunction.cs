﻿using Centazio.Core;
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

  public FinWriteFunction(SimulationCtx ctx, FinApi api) {
    this.ctx = ctx;
    this.api = api;
    Config = new(SimulationConstants.FIN_SYSTEM, LifecycleStage.Defaults.Write, [
      new(CoreEntityTypeName.From<CoreCustomer>(), TestingDefaults.CRON_EVERY_SECOND, this),
      new(CoreEntityTypeName.From<CoreInvoice>(), TestingDefaults.CRON_EVERY_SECOND, this)
    ]);
  }

  public async Task<CovertCoreEntitiesToSystemEntittiesResult> CovertCoreEntitiesToSystemEntitties(WriteOperationConfig config, List<CoreAndPendingCreateMap> tocreate, List<CoreAndPendingUpdateMap> toupdate) {
    ctx.Debug($"FinWriteFunction.CovertCoreEntitiesToSystemEntitties[{config.Object.Value}] ToCreate[{tocreate.Count}] ToUpdate[{toupdate.Count}]");
    if (config.Object.Value == nameof(CoreCustomer)) {
      return WriteHelpers.CovertCoreEntitiesToSystemEntitties<CoreCustomer>(tocreate, toupdate, ctx.ChecksumAlg, (id, e) => ctx.Converter.CoreCustomerToFinAccount(Id(id), e));
    }
    if (config.Object.Value == nameof(CoreInvoice)) {
      var cores = tocreate.Select(e => e.CoreEntity).Concat(toupdate.Select(e => e.CoreEntity)).ToList();
      var maps = await ctx.EntityMap.GetRelatedEntitySystemIdsFromCoreEntities(Config.System, CoreEntityTypeName.From<CoreCustomer>(), cores, nameof(CoreInvoice.CustomerCoreId));
      return WriteHelpers.CovertCoreEntitiesToSystemEntitties<CoreInvoice>(tocreate, toupdate, ctx.ChecksumAlg, (id, e) => ctx.Converter.CoreInvoiceToFinInvoice(Id(id), e, maps));
    }
    throw new NotSupportedException(config.Object);
    
    int Id(string id) => id == String.Empty ? 0 : Int32.Parse(id);
  }

  public async Task<WriteOperationResult> WriteEntitiesToTargetSystem(WriteOperationConfig config, List<CoreSystemAndPendingCreateMap> tocreate, List<CoreSystemAndPendingUpdateMap> toupdate) {
    ctx.Debug($"FinWriteFunction.WriteEntitiesToTargetSystem[{config.Object.Value}] Created[{tocreate.Count}] Updated[{toupdate.Count}]");
    if (config.Object.Value == nameof(CoreCustomer)) {
      var created = await api.CreateAccounts(tocreate.Select(e => e.SystemEntity.To<FinAccount>()).ToList());
      var updated = await api.UpdateAccounts(toupdate.Select(e => e.SystemEntity.To<FinAccount>()).ToList());
      return WriteHelpers.GetSuccessWriteOperationResult(tocreate, created, toupdate, updated, ctx.ChecksumAlg);
    }
    if (config.Object.Value == nameof(CoreInvoice)) {
      var created = await api.CreateInvoices(tocreate.Select(e => e.SystemEntity.To<FinInvoice>()).ToList());
      var updated = await api.UpdateInvoices(toupdate.Select(e => e.SystemEntity.To<FinInvoice>()).ToList());
      return WriteHelpers.GetSuccessWriteOperationResult(tocreate, created, toupdate, updated, ctx.ChecksumAlg);
    }
    throw new NotSupportedException(config.Object);
  }
}