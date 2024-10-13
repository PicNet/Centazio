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

  public FinPromoteFunction(SimulationCtx ctx) {
    this.ctx = ctx;
    Config = new(nameof(FinSystem), LifecycleStage.Defaults.Promote, [
      new(new(nameof(FinAccount)), CoreEntityType.From<CoreCustomer>(), TestingDefaults.CRON_EVERY_SECOND, this) { IsBidirectional = ctx.ALLOW_BIDIRECTIONAL },
      new(new(nameof(FinInvoice)), CoreEntityType.From<CoreInvoice>(), TestingDefaults.CRON_EVERY_SECOND, this) { IsBidirectional = ctx.ALLOW_BIDIRECTIONAL }
    ]);
  }
  
  public List<Containers.StagedSys> DeserialiseStagedEntities(OperationStateAndConfig<PromoteOperationConfig> config, List<StagedEntity> staged) {
    return config.State.Object.Value switch { 
      nameof(CoreCustomer) => staged.ToStagedSys<FinAccount>(), 
      nameof(CoreInvoice) => staged.ToStagedSys<FinInvoice>(), 
      _ => throw new NotSupportedException(config.State.Object) };
  }
  
  public async Task<PromoteOperationResult> BuildCoreEntities(OperationStateAndConfig<PromoteOperationConfig> config, List<Containers.StagedSysOptionalCore> staged) {
    ctx.Debug($"FinPromoteFunction[{config.OpConfig.Object.Value}] Staged[{staged.Count}]");
    var topromote = config.State.Object.Value switch { 
      nameof(CoreCustomer) => await EvaluateCustomers(), 
      nameof(CoreInvoice) => await EvaluateInvoices(), 
      _ => throw new NotSupportedException(config.State.Object) };
    return new SuccessPromoteOperationResult(topromote, []);

    Task<List<Containers.StagedSysCore>> EvaluateCustomers() {
      return Task.FromResult(staged.Select(t => 
          t.SetCore(ctx.FinAccountToCoreCustomer(t.Sys.To<FinAccount>(), t.OptCore?.To<CoreCustomer>()))).ToList());
    }

    async Task<List<Containers.StagedSysCore>> EvaluateInvoices() {
      var maps = await ctx.finhelp.GetRelatedEntityCoreIdsFromSystemIds(CoreEntityType.From<CoreCustomer>(), staged, nameof(FinInvoice.AccountId), true);
      return staged.Select(t => {
        var fininv = t.Sys.To<FinInvoice>();
        return t.SetCore(ctx.FinInvoiceToCoreInvoice(fininv, t.OptCore?.To<CoreInvoice>(), maps[fininv.AccountSystemId]));
      }).ToList();
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
      return help.CovertCoreEntitiesToSystemEntitties<CoreCustomer>(tocreate, toupdate, (id, e) => ctx.CoreCustomerToFinAccount(Id(id), e));
    }
    if (config.Object.Value == nameof(CoreInvoice)) {
      var cores = tocreate.ToCore().Concat(toupdate.ToCore()).ToList();
      var maps = await help.GetRelatedEntitySystemIdsFromCoreIds(CoreEntityType.From<CoreCustomer>(), cores, nameof(CoreInvoice.CustomerCoreId));
      return help.CovertCoreEntitiesToSystemEntitties<CoreInvoice>(tocreate, toupdate, (id, e) => ctx.CoreInvoiceToFinInvoice(Id(id), e, maps));
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
}