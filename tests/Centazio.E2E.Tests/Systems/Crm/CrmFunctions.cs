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

public class CrmReadFunction : AbstractFunction<ReadOperationConfig, ExternalEntityType, ReadOperationResult>, IGetObjectsToStage {

  public override FunctionConfig<ReadOperationConfig, ExternalEntityType> Config { get; }
  
  private readonly ICrmSystemApi api;
  
  public CrmReadFunction(ICrmSystemApi api) {
    this.api = api;
    Config = new(nameof(CrmSystem), LifecycleStage.Defaults.Read, new ([
      new (new ExternalEntityType(nameof(CrmMembershipType)), TestingDefaults.CRON_EVERY_SECOND, this),
      new (new ExternalEntityType(nameof(CrmCustomer)), TestingDefaults.CRON_EVERY_SECOND, this),
      new (new ExternalEntityType(nameof(CrmInvoice)), TestingDefaults.CRON_EVERY_SECOND, this)
    ]));
  }
  
  public async Task<ReadOperationResult> GetObjects(OperationStateAndConfig<ReadOperationConfig, ExternalEntityType> config) {
    SimulationCtx.Debug($"CrmReadFunction[{config.State.Object.Value}]");
    return config.State.Object.Value switch { 
      nameof(CrmMembershipType) => ReadOperationResult.Create(await api.GetMembershipTypes(config.Checkpoint)), 
      nameof(CrmCustomer) => ReadOperationResult.Create(await api.GetCustomers(config.Checkpoint)), 
      nameof(CrmInvoice) => ReadOperationResult.Create(await api.GetInvoices(config.Checkpoint)), 
      _ => throw new NotSupportedException(config.State.Object) 
    };
  }
}

public class CrmPromoteFunction : AbstractFunction<PromoteOperationConfig, CoreEntityType, PromoteOperationResult>, IEvaluateEntitiesToPromote {
  
  public override FunctionConfig<PromoteOperationConfig, CoreEntityType> Config { get; }
  
  private readonly CoreStorage db;

  public CrmPromoteFunction(CoreStorage db) {
    this.db = db;
    Config = new(nameof(CrmSystem), LifecycleStage.Defaults.Promote, new ([
      new (new(nameof(CrmMembershipType)), CoreEntityType.From<CoreMembershipType>(), TestingDefaults.CRON_EVERY_SECOND, this),
      new (new(nameof(CrmCustomer)), CoreEntityType.From<CoreCustomer>(), TestingDefaults.CRON_EVERY_SECOND, this),
      new (new(nameof(CrmInvoice)), CoreEntityType.From<CoreInvoice>(), TestingDefaults.CRON_EVERY_SECOND, this)
    ]));
  }

  public Task<PromoteOperationResult> Evaluate(OperationStateAndConfig<PromoteOperationConfig, CoreEntityType> config, IEnumerable<StagedEntity> staged) {
    SimulationCtx.Debug($"CrmPromoteFunction[{config.State.Object.Value}]");
    var topromote = config.State.Object.Value switch { 
      nameof(CoreMembershipType) => staged.Select(s => new StagedAndCoreEntity(s, CoreMembershipType.FromCrmMembershipType(s.Deserialise<CrmMembershipType>(), db))).ToList(), 
      nameof(CoreCustomer) => staged.Select(s => new StagedAndCoreEntity(s, CoreCustomer.FromCrmCustomer(s.Deserialise<CrmCustomer>(), db))).ToList(), 
      nameof(CoreInvoice) => staged.Select(s => new StagedAndCoreEntity(s, CoreInvoice.FromCrmInvoice(s.Deserialise<CrmInvoice>(), db))).ToList(), 
      _ => throw new NotSupportedException(config.State.Object) };
    return Task.FromResult<PromoteOperationResult>(new SuccessPromoteOperationResult(topromote, []));
  }

}

public class CrmWriteFunction : AbstractFunction<WriteOperationConfig, CoreEntityType, WriteOperationResult>, IWriteEntitiesToTargetSystem {
  
  public override FunctionConfig<WriteOperationConfig, CoreEntityType> Config { get; }
  
  private readonly CrmSystem api;
  private readonly IEntityIntraSystemMappingStore intra;

  public CrmWriteFunction(CrmSystem api, IEntityIntraSystemMappingStore intra) {
    this.api = api;
    this.intra = intra;
    Config = new(nameof(CrmSystem), LifecycleStage.Defaults.Write, new ([
      new(CoreEntityType.From<CoreCustomer>(), TestingDefaults.CRON_EVERY_SECOND, this),
      new(CoreEntityType.From<CoreInvoice>(), TestingDefaults.CRON_EVERY_SECOND, this),
    ]));
  }

  public async Task<WriteOperationResult> WriteEntities(
      WriteOperationConfig config, 
      List<CoreAndPendingCreateMap> created, 
      List<CoreAndPendingUpdateMap> updated) {
    
    SimulationCtx.Debug($"CrmWriteFunction[{config.Object.Value}]");
    
    if (config.Object.Value == nameof(CoreCustomer)) {
      var created2 = await api.CreateCustomers(created.Select(m => FromCore(Guid.Empty, m.Core.To<CoreCustomer>())).ToList());
      await api.UpdateCustomers(updated.Select(m => FromCore(Guid.Parse(m.Map.TargetId), m.Core.To<CoreCustomer>())).ToList());
      return new SuccessWriteOperationResult(
          created.Zip(created2.Select(c => c.Id.ToString())).Select(m => m.First.Created(m.Second)).ToList(),
          updated.Select(m => m.Updated()).ToList());
    }
    
    if (config.Object.Value == nameof(CoreInvoice)) {
      // simplify this very common code
      var externals = created.Select(m => m.Core)
          .Concat(updated.Select(m => m.Core))
          .Where(m => m.SourceSystem != SimulationCtx.CRM_SYSTEM.Value)
          .Cast<CoreInvoice>()
          .ToList();
      var extaccs = externals.Select(i => i.CustomerId).Distinct().ToList();
      // todo: should FindTargetIds somehow enforce uniqueness of extaccs (using Sets)?
      var targetmaps = await intra.FindTargetIds(CoreEntityType.From<CoreInvoice>(), SimulationCtx.FIN_SYSTEM, SimulationCtx.CRM_SYSTEM, extaccs);
      var created2 = await api.CreateInvoices(created.Select(m => FromCore(Guid.Empty, m.Core.To<CoreInvoice>(), targetmaps)).ToList());
      await api.UpdateInvoices(updated.Select(m => FromCore(Guid.Parse(m.Map.TargetId), m.Core.To<CoreInvoice>(), targetmaps)).ToList());
      return new SuccessWriteOperationResult(
          created.Zip(created2.Select(i => i.Id.ToString())).Select(m => m.First.Created(m.Second)).ToList(),
          updated.Select(m => m.Updated()).ToList());
    }
    
    throw new NotSupportedException(config.Object);
  }
  
  private CrmCustomer FromCore(Guid id, CoreCustomer c) => new(id, UtcDate.UtcNow, Guid.Parse(c.Membership.SourceId), c.Name);
  private CrmInvoice FromCore(Guid id, CoreInvoice i, IList<EntityIntraSysMap> custmaps) {
    var issource = i.SourceId == Config.System.Value;
    var accid = Guid.Parse(issource ? i.CustomerId : custmaps.Single(m => m.CoreId == i.CustomerId).TargetId);
    return new CrmInvoice(id, UtcDate.UtcNow, accid, i.Cents, i.DueDate, i.PaidDate);
  }

}