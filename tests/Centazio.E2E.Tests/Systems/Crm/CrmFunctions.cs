using Centazio.Core;
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
      new (new ExternalEntityType(nameof(CrmMembershipType)), TestingDefaults.CRON_EVERY_SECOND, this),
      new (new ExternalEntityType(nameof(CrmCustomer)), TestingDefaults.CRON_EVERY_SECOND, this),
      new (new ExternalEntityType(nameof(CrmInvoice)), TestingDefaults.CRON_EVERY_SECOND, this)
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
      new (new(nameof(CrmMembershipType)), TestingDefaults.CRON_EVERY_SECOND, this),
      new (new(nameof(CrmCustomer)), TestingDefaults.CRON_EVERY_SECOND, this),
      new (new(nameof(CrmInvoice)), TestingDefaults.CRON_EVERY_SECOND, this)
    ]));
  }

  public Task<PromoteOperationResult> Evaluate(OperationStateAndConfig<PromoteOperationConfig> config, IEnumerable<StagedEntity> staged) {
    var topromote = config.State.Object.Value switch { 
      nameof(CrmMembershipType) => staged.Select(s => new StagedAndCoreEntity(s, CoreMembershipType.FromCrmMembershipType(s.Deserialise<CrmMembershipType>(), db))).ToList(), 
      nameof(CrmCustomer) => staged.Select(s => new StagedAndCoreEntity(s, CoreCustomer.FromCrmCustomer(s.Deserialise<CrmCustomer>(), db))).ToList(), 
      nameof(CrmInvoice) => staged.Select(s => new StagedAndCoreEntity(s, CoreInvoice.FromCrmInvoice(s.Deserialise<CrmInvoice>(), db))).ToList(), 
      _ => throw new Exception() };
    return Task.FromResult<PromoteOperationResult>(new SuccessPromoteOperationResult(topromote, []));
  }

}

public class CrmWriteFunction : AbstractFunction<WriteOperationConfig, WriteOperationResult>, IWriteEntitiesToTargetSystem {
  
  public override FunctionConfig<WriteOperationConfig> Config { get; }
  
  private readonly CrmSystem api;
  
  public CrmWriteFunction(CrmSystem api) {
    this.api = api;
    Config = new(nameof(CrmSystem), LifecycleStage.Defaults.Write, new ([
      new(CoreEntityType.From<CoreCustomer>(), TestingDefaults.CRON_EVERY_SECOND, this),
      new(CoreEntityType.From<CoreInvoice>(), TestingDefaults.CRON_EVERY_SECOND, this),
    ]));
  }

  public async Task<WriteOperationResult> WriteEntities(
      WriteOperationConfig config, 
      List<CoreAndPendingCreateMap> created, 
      List<CoreAndPendingUpdateMap> updated) {
    
    if (config.Object == nameof(CoreCustomer)) {
      var created2 = await api.CreateCustomers(created.Select(m => FromCore(Guid.Empty, m.Core.To<CoreCustomer>())).ToList());
      await api.UpdateCustomers(updated.Select(m => FromCore(Guid.Parse(m.Map.TargetId), m.Core.To<CoreCustomer>())).ToList());
      return new SuccessWriteOperationResult(
          created.Zip(created2.Select(c => c.Id.ToString())).Select(m => m.First.Created(m.Second)).ToList(),
          updated.Select(m => m.Updated()).ToList());
    }
    
    if (config.Object == nameof(CoreInvoice)) {
      var created2 = await api.CreateInvoices(created.Select(m => FromCore(Guid.Empty, m.Core.To<CoreInvoice>())).ToList());
      await api.UpdateInvoices(updated.Select(m => FromCore(Guid.Parse(m.Map.TargetId), m.Core.To<CoreInvoice>())).ToList());
      return new SuccessWriteOperationResult(
          created.Zip(created2.Select(i => i.Id.ToString())).Select(m => m.First.Created(m.Second)).ToList(),
          updated.Select(m => m.Updated()).ToList());
    }
    
    throw new NotSupportedException(config.Object);
  }
  
  private CrmCustomer FromCore(Guid id, CoreCustomer c) => new(id, UtcDate.UtcNow, Guid.Parse(c.Membership.SourceId), c.Name);
  private CrmInvoice FromCore(Guid id, CoreInvoice i) => new(id, UtcDate.UtcNow, Guid.Parse(i.CustomerId), i.Cents, i.DueDate, i.PaidDate);

}