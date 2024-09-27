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
  
  private readonly IFinSystemApi api;
  
  public FinReadFunction(IFinSystemApi api) {
    this.api = api;
    Config = new(nameof(FinSystem), LifecycleStage.Defaults.Read, new ([
      new (nameof(FinAccount), TestingDefaults.CRON_EVERY_SECOND, this),
      new (nameof(FinInvoice), TestingDefaults.CRON_EVERY_SECOND, this)
    ]));
  }
  
  public async Task<ReadOperationResult> GetObjects(OperationStateAndConfig<ReadOperationConfig> config) => 
    config.State.Object.Value switch { 
      nameof(FinAccount) => ReadOperationResult.Create(await api.GetAccounts(config.Checkpoint)), 
      nameof(FinInvoice) => ReadOperationResult.Create(await api.GetInvoices(config.Checkpoint)), 
      _ => throw new NotSupportedException(config.State.Object) 
    };
}

public class FinPromoteFunction : AbstractFunction<PromoteOperationConfig, PromoteOperationResult>, IEvaluateEntitiesToPromote {
  
  public override FunctionConfig<PromoteOperationConfig> Config { get; }
  
  private readonly CoreStorage db;

  public FinPromoteFunction(CoreStorage db) {
    this.db = db;
    Config = new(nameof(FinSystem), LifecycleStage.Defaults.Promote, new ([
      new (nameof(FinAccount), TestingDefaults.CRON_EVERY_SECOND, this),
      new (nameof(FinInvoice), TestingDefaults.CRON_EVERY_SECOND, this)
    ]));
  }

  public Task<PromoteOperationResult> Evaluate(OperationStateAndConfig<PromoteOperationConfig> config, IEnumerable<StagedEntity> staged) {
    var topromote = config.State.Object.Value switch { 
      nameof(CoreCustomer) => staged.Select(s => new StagedAndCoreEntity(s, CoreCustomer.FromFinAccount(s.Deserialise<FinAccount>(), db))).ToList(), 
      nameof(CoreInvoice) => staged.Select(s => new StagedAndCoreEntity(s, CoreInvoice.FromFinInvoice(s.Deserialise<FinInvoice>(), db))).ToList(), 
      _ => throw new Exception() };
    return Task.FromResult<PromoteOperationResult>(new SuccessPromoteOperationResult(topromote, []));
  }

}

public class FinWriteFunction : AbstractFunction<BatchWriteOperationConfig, WriteOperationResult>, IWriteBatchEntitiesToTargetSystem {
  
  public override FunctionConfig<BatchWriteOperationConfig> Config { get; }
  
  private readonly FinSystem api;
  
  public FinWriteFunction(FinSystem api) {
    this.api = api;
    Config = new(nameof(FinSystem), LifecycleStage.Defaults.Write, new ([
      new(nameof(CoreCustomer), TestingDefaults.CRON_EVERY_SECOND, this),
      new(nameof(CoreInvoice), TestingDefaults.CRON_EVERY_SECOND, this),
    ]));
  }

  public async Task<WriteOperationResult> WriteEntities(
      BatchWriteOperationConfig config, 
      List<CoreAndPendingCreateMap> created, 
      List<CoreAndPendingUpdateMap> updated) {
    
    if (config.Object == nameof(CoreCustomer)) {
      // todo: these casts are ugly
      var created2 = await api.CreateAccounts(created.Select(m => FromCore(0, (CoreCustomer) m.Core)).ToList());
      await api.UpdateAccounts(updated.Select(m => FromCore(Int32.Parse(m.Map.TargetId), (CoreCustomer) m.Core)).ToList());
      return new SuccessWriteOperationResult(
          created.Zip(created2.Select(c => c.Id.ToString())).Select(m => m.First.Created(m.Second)).ToList(),
          updated.Select(m => m.Updated()).ToList());
    }
    
    if (config.Object == nameof(CoreInvoice)) {
      var created2 = await api.CreateInvoices(created.Select(m => FromCore(0, (CoreInvoice) m.Core)).ToList());
      await api.UpdateInvoices(updated.Select(m => FromCore(Int32.Parse(m.Map.TargetId), (CoreInvoice) m.Core)).ToList());
      return new SuccessWriteOperationResult(
          created.Zip(created2.Select(i => i.Id.ToString())).Select(m => m.First.Created(m.Second)).ToList(),
          updated.Select(m => m.Updated()).ToList());
    }
    
    throw new NotSupportedException(config.Object);
  }
  
  private FinAccount FromCore(int id, CoreCustomer c) => new(id, c.Name, UtcDate.UtcNow);
  private FinInvoice FromCore(int id, CoreInvoice i) => new(id, Int32.Parse(i.CustomerId), i.Cents / 100.0m, UtcDate.UtcNow, i.DueDate.ToDateTime(TimeOnly.MinValue), i.PaidDate);

}