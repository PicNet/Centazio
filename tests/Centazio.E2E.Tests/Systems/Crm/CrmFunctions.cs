using Centazio.Core;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.EntitySysMapping;
using Centazio.Core.Promote;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Core.Write;
using Centazio.E2E.Tests.Infra;
using Centazio.Test.Lib;

namespace Centazio.E2E.Tests.Systems.Crm;

public class CrmReadFunction : AbstractFunction<ReadOperationConfig, ReadOperationResult>, IGetObjectsToStage {

  public override FunctionConfig<ReadOperationConfig> Config { get; }
  
  private readonly CrmSystem crm;
  
  public CrmReadFunction(CrmSystem crm) {
    this.crm = crm;
    Config = new(nameof(CrmSystem), LifecycleStage.Defaults.Read, [
      new(new ExternalEntityType(nameof(CrmMembershipType)), TestingDefaults.CRON_EVERY_SECOND, this),
      new(new ExternalEntityType(nameof(CrmCustomer)), TestingDefaults.CRON_EVERY_SECOND, this),
      new(new ExternalEntityType(nameof(CrmInvoice)), TestingDefaults.CRON_EVERY_SECOND, this)
    ]);
  }
  
  public async Task<ReadOperationResult> GetUpdatesAfterCheckpoint(OperationStateAndConfig<ReadOperationConfig> config) {
    var updates = config.State.Object.Value switch { 
      nameof(CrmMembershipType) => await crm.GetMembershipTypes(config.Checkpoint), 
      nameof(CrmCustomer) => await crm.GetCustomers(config.Checkpoint), 
      nameof(CrmInvoice) => await crm.GetInvoices(config.Checkpoint), 
      _ => throw new NotSupportedException(config.State.Object) 
    };
    if (updates.Any()) SimulationCtx.Debug($"CrmReadFunction[{config.State.Object.Value}] Updates[{updates.Count}] {{{UtcDate.UtcNow:o}}}");
    return ReadOperationResult.Create(updates);
  }
}

public class CrmPromoteFunction : AbstractFunction<PromoteOperationConfig, PromoteOperationResult>, IEvaluateEntitiesToPromote {
  
  public override FunctionConfig<PromoteOperationConfig> Config { get; }
  
  private readonly CoreStorage db;

  public CrmPromoteFunction(CoreStorage db) {
    this.db = db;
    Config = new(nameof(CrmSystem), LifecycleStage.Defaults.Promote, [
      new(new(nameof(CrmMembershipType)), CoreEntityType.From<CoreMembershipType>(), TestingDefaults.CRON_EVERY_SECOND, this),
      new(new(nameof(CrmCustomer)), CoreEntityType.From<CoreCustomer>(), TestingDefaults.CRON_EVERY_SECOND, this) { IsBidirectional = SimulationCtx.ALLOW_BIDIRECTIONAL },
      new(new(nameof(CrmInvoice)), CoreEntityType.From<CoreInvoice>(), TestingDefaults.CRON_EVERY_SECOND, this) { IsBidirectional = SimulationCtx.ALLOW_BIDIRECTIONAL }
    ]);
  }

  public async Task<PromoteOperationResult> Evaluate(OperationStateAndConfig<PromoteOperationConfig> config, List<StagedEntity> staged) {
    SimulationCtx.Debug($"CrmPromoteFunction[{config.State.Object.Value}] Staged[{staged.Count}] {{{UtcDate.UtcNow:o}}}");
    var topromote = config.State.Object.Value switch { 
      nameof(CoreMembershipType) => staged.Select(s => new StagedAndCoreEntity(s, CoreMembershipType.FromCrmMembershipType(s.Deserialise<CrmMembershipType>()))).ToList(), 
      nameof(CoreCustomer) => staged.Select(s => new StagedAndCoreEntity(s, CoreCustomer.FromCrmCustomer(s.Deserialise<CrmCustomer>(), db))).ToList(), 
      nameof(CoreInvoice) => await EvaluateInvoices(), 
      _ => throw new NotSupportedException(config.State.Object) };
    return new SuccessPromoteOperationResult(topromote, []);

    async Task<List<StagedAndCoreEntity>> EvaluateInvoices() {
      var invoices = staged.Select(s => s.Deserialise<CrmInvoice>()).ToList();
      var custids = invoices.Select(i => i.CustomerId.ToString()).Distinct().ToList();
      var customers = await SimulationCtx.entitymap.GetExistingMappingsFromExternalIds(CoreEntityType.From<CoreCustomer>(), custids, config.State.System);
      var result = invoices.Zip(staged).Select(t => {
        var (crminv, se) = t;
        var custid = customers.Single(k => k.ExternalId == crminv.CustomerId.ToString()).CoreId;
        return new StagedAndCoreEntity(se, CoreInvoice.FromCrmInvoice(crminv, custid));
      }).ToList();
      return result;
    }
  }
}

public class CrmWriteFunction : AbstractFunction<WriteOperationConfig, WriteOperationResult>, IWriteEntitiesToTargetSystem {
  
  public override FunctionConfig<WriteOperationConfig> Config { get; }
  
  private readonly CrmSystem crm;
  private readonly ICoreToSystemMapStore intra;

  public CrmWriteFunction(CrmSystem crm, ICoreToSystemMapStore intra) {
    this.crm = crm;
    this.intra = intra;
    Config = new(nameof(CrmSystem), LifecycleStage.Defaults.Write, [
      new(CoreEntityType.From<CoreCustomer>(), TestingDefaults.CRON_EVERY_SECOND, this),
      new(CoreEntityType.From<CoreInvoice>(), TestingDefaults.CRON_EVERY_SECOND, this)
    ]);
  }

  public async Task<WriteOperationResult> WriteEntities(
      WriteOperationConfig config, 
      List<CoreAndPendingCreateMap> created, 
      List<CoreAndPendingUpdateMap> updated) {
    
    SimulationCtx.Debug($"CrmWriteFunction[{config.Object.Value}] Created[{created.Count}] Updated[{updated.Count}] {{{UtcDate.UtcNow:o}}}");
    if (config.Object.Value == nameof(CoreCustomer)) {
      var created2 = await crm.CreateCustomers(created.Select(m => FromCore(Guid.Empty, m.Core.To<CoreCustomer>())).ToList());
      await crm.UpdateCustomers(updated.Select(e1 => {
        var toupdate = FromCore(Guid.Parse(e1.Map.ExternalId), e1.Core.To<CoreCustomer>());
        var existing = crm.Customers.Single(e2 => e1.Map.ExternalId == e2.Id.ToString());
        if (SimulationCtx.Checksum(existing) == SimulationCtx.Checksum(toupdate)) throw new Exception($"CrmWriteFunction[{config.Object.Value}] updated object with no changes.\nExisting:\n\t{existing}\nUpdated:\n\t{toupdate}");
        return toupdate;
      }).ToList());
      return new SuccessWriteOperationResult(
          created.Zip(created2.Select(c => c.Id.ToString())).Select(m => m.First.Created(m.Second)).ToList(),
          updated.Select(m => m.Updated()).ToList());
    }
    
    if (config.Object.Value == nameof(CoreInvoice)) {
      // todo: simplify this very common code
      var externals = created.Select(m => m.Core)
          .Concat(updated.Select(m => m.Core))
          .Where(m => m.SourceSystem != SimulationCtx.CRM_SYSTEM.Value)
          .Cast<CoreInvoice>()
          .ToList();
      var externalcusts = externals.Select(i => i.CustomerId).Distinct().ToList();
      var maps = await intra.GetExistingMappingsFromCoreIds(CoreEntityType.From<CoreCustomer>(), externalcusts, SimulationCtx.FIN_SYSTEM);
      var created2 = await crm.CreateInvoices(created.Select(m => FromCore(Guid.Empty, m.Core.To<CoreInvoice>(), maps)).ToList());
      await crm.UpdateInvoices(updated.Select(e1 => {
        var toupdate = FromCore(Guid.Parse(e1.Map.ExternalId), e1.Core.To<CoreInvoice>(), maps);
        var existing = crm.Invoices.Single(e2 => e1.Map.ExternalId == e2.Id.ToString());
        if (SimulationCtx.Checksum(existing) == SimulationCtx.Checksum(toupdate)) throw new Exception($"CrmWriteFunction[{config.Object.Value}] updated object with no changes.\nExisting:\n\t{existing}\nUpdated:\n\t{toupdate}");
        return toupdate;
      }).ToList());
      return new SuccessWriteOperationResult(
          created.Zip(created2.Select(i => i.Id.ToString())).Select(m => m.First.Created(m.Second)).ToList(),
          updated.Select(m => m.Updated()).ToList());
    }
    
    throw new NotSupportedException(config.Object);
  }
  
  private CrmCustomer FromCore(Guid id, CoreCustomer c) => new(id, UtcDate.UtcNow, Guid.Parse(c.Membership.SourceId), c.Name);
  private CrmInvoice FromCore(Guid id, CoreInvoice i, List<CoreToExternalMap> custmaps) {
    var potentials = custmaps.Where(acc => acc.CoreId == i.CustomerId).ToList();
    var accid = potentials.SingleOrDefault(acc => acc.ExternalSystem == SimulationCtx.CRM_SYSTEM)?.ExternalId.Value;
    if (accid is null) {
      throw new Exception($"CrmWriteFunction -\n\t" +
          $"Could not find CoreCustomer[{i.CustomerId}] for CoreInvoice[{i.SourceId}]\n\t" +
          $"Potentials[{String.Join(",", potentials)}]\n\t" +
          $"In DB[{String.Join(",", SimulationCtx.core.Customers.Select(c => c.Id))}]");
    }
    return new CrmInvoice(id, UtcDate.UtcNow, Guid.Parse(accid), i.Cents, i.DueDate, i.PaidDate);
  }

}