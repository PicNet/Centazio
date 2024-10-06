using Centazio.Core;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.EntitySysMapping;
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
  
  public async Task<PromoteOperationResult> Evaluate(OperationStateAndConfig<PromoteOperationConfig> config, List<StagedEntity> staged) {
    ctx.Debug($"FinPromoteFunction[{config.OpConfig.Object.Value}] Staged[{staged.Count}]");

    List<Containers.StagedSysCore> topromote = new();
    if (config.State.Object.Value == nameof(CoreCustomer)) {
      // todo: this relationship building needs to be extracted into helper methods as its very common
      var accounts = staged.Deserialise<FinAccount>();
      var accsysids = accounts.Select(acc => acc.Sys.SystemId.ToString()).ToList();
      var idmaps = await ctx.entitymap.GetExistingMappingsFromSystemIds(config.State.Object.ToCoreEntityType, accsysids, config.State.System);
      foreach (var acc in accounts) {
        var coreid = idmaps.SingleOrDefault(m => m.SysId == acc.Sys.SystemId)?.CoreId;
        var existing = coreid is null ? null : ctx.core.GetCustomer(coreid); 
        var updated = ctx.FinAccountToCoreCustomer(acc.Sys.To<FinAccount>(), existing);
        topromote.Add(new Containers.StagedSysCore(acc.Staged, acc.Sys, updated)); 
      }
    } else if (config.State.Object.Value == nameof(CoreInvoice))
      await EvaluateInvoices();
    else
      throw new Exception();

    return new SuccessPromoteOperationResult(topromote, []);
    
    async Task EvaluateInvoices() {
      var invoices = staged.Select(s => s.Deserialise<FinInvoice>()).ToList();
      var accids = invoices.Select(i => i.AccountId.ToString()).Distinct().ToList();
      var accounts = await ctx.entitymap.GetExistingMappingsFromSystemIds(CoreEntityType.From<CoreCustomer>(), accids, config.State.System);
      invoices.Zip(staged).ForEach(t => {
        var (fininv, se) = t;
        var accid = accounts.Single(k => k.SysId == fininv.AccountId.ToString()).CoreId;
        topromote.Add(new Containers.StagedSysCore(se, fininv, ctx.FinInvoiceToCoreInvoice(fininv, accid)));
      });
    }
  }

}

public class FinWriteFunction : AbstractFunction<WriteOperationConfig, WriteOperationResult>, ITargetSystemWriter {
  
  public override FunctionConfig<WriteOperationConfig> Config { get; }
  
  private readonly SimulationCtx ctx;
  private readonly FinSystem fin;
  private readonly ICoreToSystemMapStore intra;

  public FinWriteFunction(SimulationCtx ctx, FinSystem fin, ICoreToSystemMapStore intra) {
    this.ctx = ctx;
    this.fin = fin;
    this.intra = intra;
    Config = new(nameof(FinSystem), LifecycleStage.Defaults.Write, [
      new(CoreEntityType.From<CoreCustomer>(), TestingDefaults.CRON_EVERY_SECOND, this),
      new(CoreEntityType.From<CoreInvoice>(), TestingDefaults.CRON_EVERY_SECOND, this)
    ]);
  }

  public async Task<(List<CoreSysAndPendingCreateMap>, List<CoreSystemMap>)> CovertCoreEntitiesToSystemEntitties(WriteOperationConfig config, List<CoreAndPendingCreateMap> tocreate, List<CoreAndPendingUpdateMap> toupdate) {
    ctx.Debug($"FinWriteFunction.CovertCoreEntitiesToSystemEntitties[{config.Object.Value}] ToCreate[{tocreate.Count}] ToUpdate[{toupdate.Count}]");
    if (config.Object.Value == nameof(CoreCustomer)) {
      return (
          tocreate.Select(m => {
            var sysent = FromCore(0, m.Core.To<CoreCustomer>()); 
            return m.AddSystemEntity(sysent, ctx.checksum.Checksum(sysent));
          }).ToList(),
          toupdate.Select(m => m.SetSystemEntity(FromCore(Int32.Parse(m.Map.SysId), m.Core.To<CoreCustomer>()))).ToList());
    }
    if (config.Object.Value == nameof(CoreInvoice)) {
      var custids = toupdate.Select(m => m.Core.To<CoreInvoice>().CustomerId).ToList();
      var maps = await intra.GetExistingMappingsFromCoreIds(CoreEntityType.From<CoreCustomer>(), custids, SimulationConstants.FIN_SYSTEM);
      return (
          tocreate.Select(m => {
            var sysent = FromCore(0, m.Core.To<CoreInvoice>(), maps); 
            return m.AddSystemEntity(sysent, ctx.checksum.Checksum(sysent));
          }).ToList(),
          toupdate.Select(m => m.SetSystemEntity(FromCore(Int32.Parse(m.Map.SysId), m.Core.To<CoreInvoice>(), maps))).ToList());
    }
    
    throw new NotSupportedException(config.Object);
  }

  public async Task<WriteOperationResult> WriteEntitiesToTargetSystem(WriteOperationConfig config, List<CoreSysAndPendingCreateMap> created, List<CoreSystemMap> updated) {
    
    ctx.Debug($"FinWriteFunction.WriteEntitiesToTargetSystem[{config.Object.Value}] Created[{created.Count}] Updated[{updated.Count}]");
    
    if (config.Object.Value == nameof(CoreCustomer)) {
      var created2 = await fin.CreateAccounts(created.Select(m => FromCore(0, m.Core.To<CoreCustomer>())).ToList());
      await fin.UpdateAccounts(updated.Select(e1 => {
        var toupdate = e1.SystemEntity.To<FinAccount>();
        var existing = fin.Accounts.Single(e2 => e1.Map.SysId == e2.SystemId.ToString());
        if (ctx.checksum.Checksum(existing) == ctx.checksum.Checksum(toupdate)) throw new Exception($"FinWriteFunction[{config.Object.Value}] updated object with no changes.\nExisting:\n\t{existing}\nUpdated:\n\t{toupdate}");
        return toupdate;
      }).ToList());
      return new SuccessWriteOperationResult(
          created.Zip(created2.Select(c => c.SystemId.ToString())).Select(m => {
            var cmap = m.First.Map.SuccessCreate(m.Second, new(Guid.NewGuid().ToString()));
            return new CoreAndCreatedMap(m.First.Core, cmap);
          }).ToList(),
          updated.Select(m => m.Updated(ctx.checksum.Checksum(m.SystemEntity))).ToList());
    }
    
    if (config.Object.Value == nameof(CoreInvoice)) {
      // todo: this process of getting related entity accounts needs to be streamlined with own utility type/method
      // note: to get the correct target ids for writing back to the target system, we only need to get the mapping
      //    for entities created in another system.  Those created by this target system can just use the `SourceId`
      var customers = created.Select(m => m.Core.To<CoreInvoice>().CustomerId)
          .Distinct()
          .ToList();
      var maps = await intra.GetExistingMappingsFromCoreIds(CoreEntityType.From<CoreCustomer>(), customers, SimulationConstants.FIN_SYSTEM);
      var created2 = await fin.CreateInvoices(created.Select(m => FromCore(0, m.Core.To<CoreInvoice>(), maps)).ToList());
      await fin.UpdateInvoices(updated.Select(e1 => {
        var toupdate = e1.SystemEntity.To<FinInvoice>();
        var existing = fin.Invoices.Single(e2 => e1.Map.SysId == e2.SystemId.ToString());
        if (ctx.checksum.Checksum(existing) == ctx.checksum.Checksum(toupdate)) throw new Exception($"FinWriteFunction[{config.Object.Value}] updated object with no changes.\nExisting:\n\t{existing}\nUpdated:\n\t{toupdate}");
        return toupdate;
      }).ToList());
      return new SuccessWriteOperationResult(
          created.Zip(created2.Select(i => i.SystemId.ToString())).Select(m => {
            var cmap = m.First.Map.SuccessCreate(m.Second, new(Guid.NewGuid().ToString()));
            return new CoreAndCreatedMap(m.First.Core, cmap);
          }).ToList(),
          updated.Select(m => m.Updated(ctx.checksum.Checksum(m.SystemEntity))).ToList());
    }
    
    throw new NotSupportedException(config.Object);
  }
  
  private FinAccount FromCore(int id, CoreCustomer c) => new(id, c.Name, UtcDate.UtcNow);
  private FinInvoice FromCore(int id, CoreInvoice i, List<CoreToSystemMap> accmaps) {
    var potentials = accmaps.Where(acc => acc.CoreId == i.CustomerId).ToList();
    var accid = potentials.SingleOrDefault(acc => acc.System == SimulationConstants.FIN_SYSTEM)?.SysId.Value;
    if (accid is null) {
      throw new Exception($"FinWriteFunction -\n\t" +
          $"Could not find CoreCustomer[{i.CustomerId}] for CoreInvoice[{i.SourceId}]({i})\n\t" +
          $"Potentials[{String.Join(",", potentials)}]\n\t" +
          $"In DB[{String.Join(",", ctx.core.Customers.Select(c => c.Id))}]");
    }
    return new(id, Int32.Parse(accid), i.Cents / 100.0m, UtcDate.UtcNow, i.DueDate.ToDateTime(TimeOnly.MinValue), i.PaidDate);
  }
}