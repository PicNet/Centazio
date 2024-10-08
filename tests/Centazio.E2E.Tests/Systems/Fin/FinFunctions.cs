using Centazio.Core;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Promote;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Core.Write;
using Centazio.E2E.Tests.Infra;
using Centazio.Test.Lib;

namespace Centazio.E2E.Tests.Systems.Fin;

public class FinReadFunction : AbstractFunction<ReadOperationConfig, ReadOperationResult>, IGetObjectsToStage {

  public override FunctionConfig<ReadOperationConfig> Config { get; }
  
  private readonly SimulationCtx ctx;
  private readonly FinSystem fin;
  
  public FinReadFunction(SimulationCtx ctx, FinSystem fin) {
    this.ctx = ctx;
    this.fin = fin;
    Config = new(nameof(FinSystem), LifecycleStage.Defaults.Read, [
      new(SystemEntityType.From<FinAccount>(), TestingDefaults.CRON_EVERY_SECOND, this),
      new(SystemEntityType.From<FinInvoice>(), TestingDefaults.CRON_EVERY_SECOND, this)
    ]);
  }
  
  public async Task<ReadOperationResult> GetUpdatesAfterCheckpoint(OperationStateAndConfig<ReadOperationConfig> config) {
    var updates = config.State.Object.Value switch { 
      nameof(FinAccount) => await fin.GetAccounts(config.Checkpoint), 
      nameof(FinInvoice) => await fin.GetInvoices(config.Checkpoint), 
      _ => throw new NotSupportedException(config.State.Object) 
    }; 
    ctx.Debug($"FinReadFunction[{config.OpConfig.Object.Value}] Updates[{updates.Count}]");
    return ReadOperationResult.Create(updates);
  }
}

public class FinPromoteFunction : AbstractFunction<PromoteOperationConfig, PromoteOperationResult>, IEvaluateEntitiesToPromote {
  
  public override FunctionConfig<PromoteOperationConfig> Config { get; }
  
  private readonly SimulationCtx ctx;
  private readonly FunctionHelpers help;

  public FinPromoteFunction(SimulationCtx ctx) {
    this.ctx = ctx;
    Config = new(nameof(FinSystem), LifecycleStage.Defaults.Promote, [
      new(new(nameof(FinAccount)), CoreEntityType.From<CoreCustomer>(), TestingDefaults.CRON_EVERY_SECOND, this) { IsBidirectional = ctx.ALLOW_BIDIRECTIONAL },
      new(new(nameof(FinInvoice)), CoreEntityType.From<CoreInvoice>(), TestingDefaults.CRON_EVERY_SECOND, this) { IsBidirectional = ctx.ALLOW_BIDIRECTIONAL }
    ]);
    help = new FunctionHelpers(SimulationConstants.FIN_SYSTEM, ctx.checksum, ctx.entitymap);
  }
  
  public async Task<PromoteOperationResult> Evaluate(OperationStateAndConfig<PromoteOperationConfig> config, List<StagedEntity> staged) {
    ctx.Debug($"FinPromoteFunction[{config.OpConfig.Object.Value}] Staged[{staged.Count}]");
    var topromote = config.State.Object.Value switch { 
      nameof(CoreCustomer) => await EvaluateCustomers(), 
      nameof(CoreInvoice) => await EvaluateInvoices(), 
      _ => throw new NotSupportedException(config.State.Object) };
    return new SuccessPromoteOperationResult(topromote, []);

    async Task<List<Containers.StagedSysCore>> EvaluateCustomers() {
      var accounts = staged.Deserialise<FinAccount>();
      var maps = await help.GetRelatedEntityCoreIdsFromSystemIds(staged.ToSysEnt<FinAccount>(), nameof(ISystemEntity.SystemId), CoreEntityType.From<CoreCustomer>());
      return accounts.Select(acc => {
        maps.TryGetValue(acc.Sys.SystemId, out var coreid);
        var existing = coreid is null ? null : ctx.core.GetCustomer(coreid); 
        var updated = ctx.FinAccountToCoreCustomer(acc.Sys.To<FinAccount>(), existing);
        return new Containers.StagedSysCore(acc.Staged, acc.Sys, updated);
      }).ToList();
    }

    async Task<List<Containers.StagedSysCore>> EvaluateInvoices() {
      var maps = await help.GetRelatedEntityCoreIdsFromSystemIds(staged.ToSysEnt<FinInvoice>(), nameof(FinInvoice.AccountId), CoreEntityType.From<CoreCustomer>());
      return staged.ToStagedSysCore<FinInvoice>(e => ctx.FinInvoiceToCoreInvoice(e, maps[e.AccountId.ToString()]));
    }
  }

}

public class FinWriteFunction : AbstractFunction<WriteOperationConfig, WriteOperationResult>, ITargetSystemWriter {
  
  public override FunctionConfig<WriteOperationConfig> Config { get; }
  
  private readonly SimulationCtx ctx;
  private readonly FinSystem fin;
  private readonly FunctionHelpers help;

  public FinWriteFunction(SimulationCtx ctx, FinSystem fin) {
    this.ctx = ctx;
    this.fin = fin;
    help = new(SimulationConstants.FIN_SYSTEM, ctx.checksum, ctx.entitymap);
    Config = new(SimulationConstants.FIN_SYSTEM, LifecycleStage.Defaults.Write, [
      new(CoreEntityType.From<CoreCustomer>(), TestingDefaults.CRON_EVERY_SECOND, this),
      new(CoreEntityType.From<CoreInvoice>(), TestingDefaults.CRON_EVERY_SECOND, this)
    ]);
  }

  public async Task<(List<CoreSystemAndPendingCreateMap>, List<CoreSystemAndPendingUpdateMap>)> CovertCoreEntitiesToSystemEntitties(WriteOperationConfig config, List<CoreAndPendingCreateMap> tocreate, List<CoreAndPendingUpdateMap> toupdate) {
    ctx.Debug($"FinWriteFunction.CovertCoreEntitiesToSystemEntitties[{config.Object.Value}] ToCreate[{tocreate.Count}] ToUpdate[{toupdate.Count}]");
    if (config.Object.Value == nameof(CoreCustomer)) {
      return help.CovertCoreEntitiesToSystemEntitties<CoreCustomer>(tocreate, toupdate, (id, e) => FromCore(Id(id), e));
    }
    if (config.Object.Value == nameof(CoreInvoice)) {
      var cores = tocreate.ToCore().Concat(toupdate.ToCore()).ToList();
      var maps = await help.GetRelatedEntitySystemIdsFromCoreIds(cores, nameof(CoreInvoice.CustomerId), CoreEntityType.From<CoreCustomer>());
      return help.CovertCoreEntitiesToSystemEntitties<CoreInvoice>(tocreate, toupdate, (id, e) => FromCore(Id(id), e, maps));
    }
    throw new NotSupportedException(config.Object);
    
    int Id(string id) => id == String.Empty ? 0 : Int32.Parse(id);
  }

  public async Task<WriteOperationResult> WriteEntitiesToTargetSystem(WriteOperationConfig config, List<CoreSystemAndPendingCreateMap> tocreate, List<CoreSystemAndPendingUpdateMap> toupdate) {
    ctx.Debug($"FinWriteFunction.WriteEntitiesToTargetSystem[{config.Object.Value}] Created[{tocreate.Count}] Updated[{toupdate.Count}]");
    if (config.Object.Value == nameof(CoreCustomer)) {
      var (created, updated) = (await fin.CreateAccounts(tocreate.ToSysEnt<FinAccount>()), await fin.UpdateAccounts(toupdate.ToSysEnt<FinAccount>()));
      return new SuccessWriteOperationResult(tocreate.SuccessCreate(created, ctx.checksum), toupdate.SuccessUpdate(updated, ctx.checksum));
    }
    if (config.Object.Value == nameof(CoreInvoice)) {
      var (created, updated) = (await fin.CreateInvoices(tocreate.ToSysEnt<FinInvoice>()), await fin.UpdateInvoices(toupdate.ToSysEnt<FinInvoice>()));
      return new SuccessWriteOperationResult(tocreate.SuccessCreate(created, ctx.checksum), toupdate.SuccessUpdate(updated, ctx.checksum));
    }
    throw new NotSupportedException(config.Object);
  }
  
  private FinAccount FromCore(int id, CoreCustomer c) => new(id, c.Name, UtcDate.UtcNow);
  private FinInvoice FromCore(int id, CoreInvoice i, Dictionary<ValidString, ValidString> accmaps) => 
      new(id, Int32.Parse(accmaps[i.CustomerId]), i.Cents / 100.0m, UtcDate.UtcNow, i.DueDate.ToDateTime(TimeOnly.MinValue), i.PaidDate);

}