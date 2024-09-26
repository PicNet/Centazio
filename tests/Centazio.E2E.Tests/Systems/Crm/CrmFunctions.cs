using Centazio.Core;
using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.Promote;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Core.Write;
using Centazio.E2E.Tests.Infra;
using Centazio.Test.Lib;

namespace Centazio.E2E.Tests.Systems.Crm;

public class CrmReadFunction : AbstractFunction<ReadOperationConfig, ReadOperationResult>, IGetObjectsToStage {

  public override FunctionConfig<ReadOperationConfig> Config { get; }
  
  private readonly ICrmSystemApi api;
  
  public CrmReadFunction(ICrmSystemApi api) {
    this.api = api;
    Config = new(nameof(CrmSystem), LifecycleStage.Defaults.Read, new ([
      new (nameof(CrmMembershipType), TestingDefaults.CRON_EVERY_SECOND, this),
      new (nameof(CrmCustomer), TestingDefaults.CRON_EVERY_SECOND, this),
      new (nameof(CrmInvoice), TestingDefaults.CRON_EVERY_SECOND, this)
    ]));
  }
  
  public async Task<ReadOperationResult> GetObjects(OperationStateAndConfig<ReadOperationConfig> config) => 
    config.State.Object.Value switch { 
      nameof(CrmMembershipType) => ReadOperationResult.Create(await api.GetMembershipTypes(config.Checkpoint)), 
      nameof(CrmCustomer) => ReadOperationResult.Create(await api.GetCustomers(config.Checkpoint)), 
      nameof(CrmInvoice) => ReadOperationResult.Create(await api.GetInvoices(config.Checkpoint)), 
      _ => throw new NotSupportedException(config.State.Object) 
    };
}

public class CrmPromoteFunction : AbstractFunction<PromoteOperationConfig, PromoteOperationResult>, IEvaluateEntitiesToPromote {
  
  public override FunctionConfig<PromoteOperationConfig> Config { get; }
  
  private readonly CoreStorage db;

  public CrmPromoteFunction(CoreStorage db) {
    this.db = db;
    Config = new(nameof(CrmSystem), LifecycleStage.Defaults.Promote, new ([
      new (nameof(CoreMembershipType), TestingDefaults.CRON_EVERY_SECOND, this),
      new (nameof(CoreCustomer), TestingDefaults.CRON_EVERY_SECOND, this),
      new (nameof(CoreInvoice), TestingDefaults.CRON_EVERY_SECOND, this)
    ]));
  }

  public Task<PromoteOperationResult> Evaluate(OperationStateAndConfig<PromoteOperationConfig> config, IEnumerable<StagedEntity> staged) {
    var topromote = config.State.Object.Value switch { 
      nameof(CoreMembershipType) => staged.Select(s => (s, (ICoreEntity) CoreMembershipType.FromCrmMembershipType(s.Deserialise<CrmMembershipType>(), db))).ToList(), 
      nameof(CoreCustomer) => staged.Select(s => (s, (ICoreEntity) CoreCustomer.FromCrmCustomer(s.Deserialise<CrmCustomer>(), db))).ToList(), 
      nameof(CoreInvoice) => staged.Select(s => (s, (ICoreEntity) CoreInvoice.FromCrmInvoice(s.Deserialise<CrmInvoice>(), db))).ToList(), 
      _ => throw new Exception() };
    return Task.FromResult<PromoteOperationResult>(new SuccessPromoteOperationResult(topromote, []));
  }

}

public class CrmWriteFunction : AbstractFunction<BatchWriteOperationConfig, WriteOperationResult>, IWriteBatchEntitiesToTargetSystem {
  
  public override FunctionConfig<BatchWriteOperationConfig> Config { get; }
  
  private CrmSystem api;
  
  public CrmWriteFunction(CrmSystem api) {
    this.api = api;
    Config = new(nameof(CrmSystem), LifecycleStage.Defaults.Write, new ([
      new(nameof(CoreCustomer), TestingDefaults.CRON_EVERY_SECOND, this),
      new(nameof(CoreInvoice), TestingDefaults.CRON_EVERY_SECOND, this),
    ]));
  }

  public async Task<WriteOperationResult> WriteEntities(
      BatchWriteOperationConfig config, 
      // todo: these tuples need actual record types to make working with them easier
      List<(ICoreEntity Core, EntityIntraSysMap.PendingCreate Map)> created, 
      List<(ICoreEntity Core, EntityIntraSysMap.PendingUpdate Map)> updated) {
    
    if (config.Object == nameof(CoreCustomer)) {
      // todo: these casts are ugly
      var created2 = await api.CreateCustomers(created.Select(m => FromCore(Guid.Empty, (CoreCustomer) m.Core)).ToList());
      await api.UpdateCustomers(created.Select(m => FromCore(Guid.Parse(m.Map.TargetId), (CoreCustomer) m.Core)).ToList());
      return new SuccessWriteOperationResult(
          created.Zip(created2.Select(c => c.Id.ToString())).Select(m => (m.First.Core, Map: m.First.Map.SuccessCreate(m.Second))).ToList(),
          updated.Select(m => (m.Core, Map: m.Map.SuccessUpdate())).ToList());
    }
    
    if (config.Object == nameof(CoreInvoice)) {
      var created2 = await api.CreateInvoices(created.Select(m => FromCore(Guid.Empty, (CoreInvoice) m.Core)).ToList());
      await api.UpdateInvoices(created.Select(m => FromCore(Guid.Parse(m.Map.TargetId), (CoreInvoice) m.Core)).ToList());
      return new SuccessWriteOperationResult(
          created.Zip(created2.Select(i => i.Id.ToString())).Select(m => (m.First.Core, Map: m.First.Map.SuccessCreate(m.Second))).ToList(),
          updated.Select(m => (m.Core, Map: m.Map.SuccessUpdate())).ToList());
    }
    
    throw new NotSupportedException(config.Object);
  }
  
  // todo: handle relationships
  private CrmCustomer FromCore(Guid id, CoreCustomer c) => new(id, UtcDate.UtcNow, Guid.Empty, c.Name);
  private CrmInvoice FromCore(Guid id, CoreInvoice i) => new(id, UtcDate.UtcNow, Guid.Empty, i.Cents, i.DueDate, i.PaidDate);

}