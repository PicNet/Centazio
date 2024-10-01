﻿using Centazio.Core;
using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.EntitySysMapping;
using Centazio.Core.Promote;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Core.Write;
using Centazio.E2E.Tests.Infra;
using Centazio.Test.Lib;

namespace Centazio.E2E.Tests.Systems.Fin;

public class FinReadFunction : AbstractFunction<ReadOperationConfig, ExternalEntityType, ReadOperationResult>, IGetObjectsToStage {

  public override FunctionConfig<ReadOperationConfig, ExternalEntityType> Config { get; }
  
  private readonly IFinSystemApi api;
  
  public FinReadFunction(IFinSystemApi api) {
    this.api = api;
    Config = new(nameof(FinSystem), LifecycleStage.Defaults.Read, new ([
      new (new(nameof(FinAccount)), TestingDefaults.CRON_EVERY_SECOND, this),
      new (new(nameof(FinInvoice)), TestingDefaults.CRON_EVERY_SECOND, this)
    ]));
  }
  
  public async Task<ReadOperationResult> GetUpdatesAfterCheckpoint(OperationStateAndConfig<ReadOperationConfig, ExternalEntityType> config) {
    var updates = config.State.Object.Value switch { 
      nameof(FinAccount) => await api.GetAccounts(config.Checkpoint), 
      nameof(FinInvoice) => await api.GetInvoices(config.Checkpoint), 
      _ => throw new NotSupportedException(config.State.Object) 
    }; 
    SimulationCtx.Debug($"FinReadFunction[{config.Config.Object.Value}] Updates[{updates.Count}] {{{UtcDate.UtcNow:o}}}");
    return ReadOperationResult.Create(updates);
  }
}

public class FinPromoteFunction : AbstractFunction<PromoteOperationConfig, CoreEntityType, PromoteOperationResult>, IEvaluateEntitiesToPromote {
  
  public override FunctionConfig<PromoteOperationConfig, CoreEntityType> Config { get; }
  
  private readonly CoreStorage db;

  public FinPromoteFunction(CoreStorage db) {
    this.db = db;
    Config = new(nameof(FinSystem), LifecycleStage.Defaults.Promote, new ([
      new (new(nameof(FinAccount)), CoreEntityType.From<CoreCustomer>(), TestingDefaults.CRON_EVERY_SECOND, this) { IsBidirectional = SimulationCtx.ALLOW_BIDIRECTIONAL },
      new (new(nameof(FinInvoice)), CoreEntityType.From<CoreInvoice>(), TestingDefaults.CRON_EVERY_SECOND, this) { IsBidirectional = SimulationCtx.ALLOW_BIDIRECTIONAL }
    ]));
  }

  public async Task<PromoteOperationResult> Evaluate(OperationStateAndConfig<PromoteOperationConfig, CoreEntityType> config, List<StagedEntity> staged) {
    SimulationCtx.Debug($"FinPromoteFunction[{config.Config.Object.Value}] Staged[{staged.Count}] {{{UtcDate.UtcNow:o}}}");
    
    var topromote = config.State.Object.Value switch { 
      nameof(CoreCustomer) => staged.Select(s => new StagedAndCoreEntity(s, CoreCustomer.FromFinAccount(s.Deserialise<FinAccount>(), db))).ToList(), 
      nameof(CoreInvoice) => await staged.Select(async s => {
        var fininv = s.Deserialise<FinInvoice>();
        // todo: this needs to be cleaned up 
        var custid = await SimulationCtx.entitymap.GetCoreIdForSystem(CoreEntityType.From<CoreCustomer>(), fininv.AccountId.ToString(), config.State.System)
            ?? SimulationCtx.core.Customers.Single(c => c.SourceId == fininv.AccountId.ToString()).Id;
        return new StagedAndCoreEntity(s, CoreInvoice.FromFinInvoice(fininv, custid));
      }).Synchronous(), 
      _ => throw new Exception() };
    return new SuccessPromoteOperationResult(topromote, []);
  }

}

public class FinWriteFunction : AbstractFunction<WriteOperationConfig, CoreEntityType, WriteOperationResult>, IWriteEntitiesToTargetSystem {
  
  public override FunctionConfig<WriteOperationConfig, CoreEntityType> Config { get; }
  
  private readonly FinSystem api;
  private readonly IEntityIntraSystemMappingStore intra;

  public FinWriteFunction(FinSystem api, IEntityIntraSystemMappingStore intra) {
    this.api = api;
    this.intra = intra;
    Config = new(nameof(FinSystem), LifecycleStage.Defaults.Write, new ([
      new(CoreEntityType.From<CoreCustomer>(), TestingDefaults.CRON_EVERY_SECOND, this),
      new(CoreEntityType.From<CoreInvoice>(), TestingDefaults.CRON_EVERY_SECOND, this),
    ]));
  }

  public async Task<WriteOperationResult> WriteEntities(
      WriteOperationConfig config, 
      List<CoreAndPendingCreateMap> created, 
      List<CoreAndPendingUpdateMap> updated) {
    
    SimulationCtx.Debug($"FinWriteFunction[{config.Object.Value}] Created[{created.Count}] Updated[{updated.Count}] {{{UtcDate.UtcNow:o}}}");
    
    if (config.Object.Value == nameof(CoreCustomer)) {
      var created2 = await api.CreateAccounts(created.Select(m => FromCore(0, m.Core.To<CoreCustomer>())).ToList());
      await api.UpdateAccounts(updated.Select(e1 => {
        var toupdate = FromCore(Int32.Parse(e1.Map.ExternalId), e1.Core.To<CoreCustomer>());
        var existing = api.Accounts.Single(e2 => e1.Map.ExternalId == e2.Id.ToString());
        if (SimulationCtx.Checksum(existing) == SimulationCtx.Checksum(toupdate)) throw new Exception($"FinWriteFunction[{config.Object.Value}] updated object with no changes.  Existing[{existing}] Updated[{toupdate}]");
        return toupdate;
      }).ToList());
      return new SuccessWriteOperationResult(
          created.Zip(created2.Select(c => c.Id.ToString())).Select(m => m.First.Created(m.Second)).ToList(),
          updated.Select(m => m.Updated()).ToList());
    }
    
    if (config.Object.Value == nameof(CoreInvoice)) {
      // todo: this process of getting related entity accounts needs to be streamlined with own utility type/method
      // note: to get the correct target ids for writing back to the target system, we only need to get the mapping
      //    for entities created in another system.  Those created by this target system can just use the `SourceId`
      var customers = created.Select(m => m.Core.To<CoreInvoice>().CustomerId)
          .Concat(updated.Select(m => m.Core.To<CoreInvoice>().CustomerId))
          .Distinct()
          .ToList();
      // todo: should FindTargetIds somehow enforce uniqueness of extaccs (using Sets)?
      var maps = await intra.FindTargetIds(CoreEntityType.From<CoreCustomer>(), SimulationCtx.FIN_SYSTEM, customers);
      // todo: clean up `existingcores`
      var existingcores = SimulationCtx.core.Customers.Where(c => customers.Contains(c.Id)).ToList();
      var created2 = await api.CreateInvoices(created.Select(m => FromCore(0, m.Core.To<CoreInvoice>(), maps, existingcores)).ToList());
      await api.UpdateInvoices(updated.Select(e1 => {
        var toupdate = FromCore(Int32.Parse(e1.Map.ExternalId), e1.Core.To<CoreInvoice>(), maps, existingcores);
        var existing = api.Invoices.Single(e2 => e1.Map.ExternalId == e2.Id.ToString());
        if (SimulationCtx.Checksum(existing) == SimulationCtx.Checksum(toupdate)) throw new Exception($"FinWriteFunction[{config.Object.Value}] updated object with no changes.  Existing[{existing}] Updated[{toupdate}]");
        return toupdate;
      }).ToList());
      return new SuccessWriteOperationResult(
          created.Zip(created2.Select(i => i.Id.ToString())).Select(m => m.First.Created(m.Second)).ToList(),
          updated.Select(m => m.Updated()).ToList());
    }
    
    throw new NotSupportedException(config.Object);
  }
  
  private FinAccount FromCore(int id, CoreCustomer c) => new(id, c.Name, UtcDate.UtcNow);
  private FinInvoice FromCore(int id, CoreInvoice i, List<CoreToExternalMap> accmaps, List<ICoreEntity> existingcores) {
    var potentials = accmaps.Where(acc => acc.CoreId == i.CustomerId).ToList();
    var accid = potentials.SingleOrDefault(acc => acc.ExternalSystem == SimulationCtx.FIN_SYSTEM)?.ExternalId.Value ??
        existingcores.SingleOrDefault(c => c.Id == i.CustomerId)?.SourceId;
    if (accid is null) {
      throw new Exception($"FinWriteFunction -\n\t" +
          $"Could not find CoreCustomer[{i.CustomerId}] for CoreInvoice[{i.SourceId}]({i})\n\t" +
          $"Potentials[{String.Join(",", potentials)}]\n\t" +
          $"Existing Cores[{String.Join(",", existingcores.Select(c => c.Id))}]\n\t" +
          $"In DB[{String.Join(",", SimulationCtx.core.Customers.Select(c => c.Id))}]");
    }
    return new(id, Int32.Parse(accid), i.Cents / 100.0m, UtcDate.UtcNow, i.DueDate.ToDateTime(TimeOnly.MinValue), i.PaidDate);
  }

}