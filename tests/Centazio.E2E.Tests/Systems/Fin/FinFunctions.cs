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
  
  public async Task<ReadOperationResult> GetObjects(OperationStateAndConfig<ReadOperationConfig, ExternalEntityType> config) {
    SimulationCtx.Debug($"FinReadFunction[{config.Config.Object.Value}]");
    return config.State.Object.Value switch { 
      nameof(FinAccount) => ReadOperationResult.Create(await api.GetAccounts(config.Checkpoint)), 
      nameof(FinInvoice) => ReadOperationResult.Create(await api.GetInvoices(config.Checkpoint)), 
      _ => throw new NotSupportedException(config.State.Object) 
    }; 
  }
}

public class FinPromoteFunction : AbstractFunction<PromoteOperationConfig, CoreEntityType, PromoteOperationResult>, IEvaluateEntitiesToPromote {
  
  public override FunctionConfig<PromoteOperationConfig, CoreEntityType> Config { get; }
  
  private readonly CoreStorage db;

  public FinPromoteFunction(CoreStorage db) {
    this.db = db;
    Config = new(nameof(FinSystem), LifecycleStage.Defaults.Promote, new ([
      new (new(nameof(FinAccount)), CoreEntityType.From<CoreCustomer>(), TestingDefaults.CRON_EVERY_SECOND, this),
      new (new(nameof(FinInvoice)), CoreEntityType.From<CoreInvoice>(), TestingDefaults.CRON_EVERY_SECOND, this)
    ]));
  }

  public Task<PromoteOperationResult> Evaluate(OperationStateAndConfig<PromoteOperationConfig, CoreEntityType> config, IEnumerable<StagedEntity> staged) {
    SimulationCtx.Debug($"FinPromoteFunction[{config.Config.Object.Value}]");
    
    var topromote = config.State.Object.Value switch { 
      nameof(CoreCustomer) => staged.Select(s => new StagedAndCoreEntity(s, CoreCustomer.FromFinAccount(s.Deserialise<FinAccount>(), db))).ToList(), 
      nameof(CoreInvoice) => staged.Select(s => new StagedAndCoreEntity(s, CoreInvoice.FromFinInvoice(s.Deserialise<FinInvoice>(), db))).ToList(), 
      _ => throw new Exception() };
    return Task.FromResult<PromoteOperationResult>(new SuccessPromoteOperationResult(topromote, []));
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
    
    SimulationCtx.Debug($"FinWriteFunction[{config.Object.Value}]");
    
    if (config.Object.Value == nameof(CoreCustomer)) {
      var created2 = await api.CreateAccounts(created.Select(m => FromCore(0, m.Core.To<CoreCustomer>())).ToList());
      await api.UpdateAccounts(updated.Select(m => FromCore(Int32.Parse(m.Map.TargetId), m.Core.To<CoreCustomer>())).ToList());
      return new SuccessWriteOperationResult(
          created.Zip(created2.Select(c => c.Id.ToString())).Select(m => m.First.Created(m.Second)).ToList(),
          updated.Select(m => m.Updated()).ToList());
    }
    
    if (config.Object.Value == nameof(CoreInvoice)) {
      // todo: this process of getting related entity accounts needs to be streamlined with own utility type/method
      // note: to get the correct target ids for writing back to the target system, we only need to get the mapping
      //    for entities created in another system.  Those created by this target system can just use the `SourceId`
      var externals = created.Select(m => m.Core)
          .Concat(updated.Select(m => m.Core))
          .Where(m => m.SourceSystem != SimulationCtx.FIN_SYSTEM.Value)
          .Cast<CoreInvoice>()
          .ToList();
      var extaccs = externals.Select(i => i.CustomerId).Distinct().ToList();
      // todo: should FindTargetIds somehow enforce uniqueness of extaccs (using Sets)?
      var targetmaps = await intra.FindTargetIds(CoreEntityType.From<CoreInvoice>(), SimulationCtx.CRM_SYSTEM, SimulationCtx.FIN_SYSTEM, extaccs);
      var created2 = await api.CreateInvoices(created.Select(m => FromCore(0, m.Core.To<CoreInvoice>(), targetmaps)).ToList());
      await api.UpdateInvoices(updated.Select(m => FromCore(Int32.Parse(m.Map.TargetId), m.Core.To<CoreInvoice>(), targetmaps)).ToList());
      return new SuccessWriteOperationResult(
          created.Zip(created2.Select(i => i.Id.ToString())).Select(m => m.First.Created(m.Second)).ToList(),
          updated.Select(m => m.Updated()).ToList());
    }
    
    throw new NotSupportedException(config.Object);
  }
  
  private FinAccount FromCore(int id, CoreCustomer c) => new(id, c.Name, UtcDate.UtcNow);
  private FinInvoice FromCore(int id, CoreInvoice i, IList<EntityIntraSysMap> accmaps) {
    var issource = i.SourceId == Config.System.Value;
    var accid = Int32.Parse(issource ? i.CustomerId : accmaps.Single(m => m.CoreId == i.CustomerId).TargetId);
    return new(id, accid, i.Cents / 100.0m, UtcDate.UtcNow, i.DueDate.ToDateTime(TimeOnly.MinValue), i.PaidDate);
  }

}