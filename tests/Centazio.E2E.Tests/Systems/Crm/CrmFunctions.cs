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
  
  private readonly SimulationCtx ctx;
  private readonly CrmSystem crm;
  
  public CrmReadFunction(SimulationCtx ctx, CrmSystem crm) {
    this.ctx = ctx;
    this.crm = crm;
    
    Config = new(nameof(CrmSystem), LifecycleStage.Defaults.Read, [
      new(SystemEntityType.From<CrmMembershipType>(), TestingDefaults.CRON_EVERY_SECOND, this),
      new(SystemEntityType.From<CrmCustomer>(), TestingDefaults.CRON_EVERY_SECOND, this),
      new(SystemEntityType.From<CrmInvoice>(), TestingDefaults.CRON_EVERY_SECOND, this)
    ]);
  }
  
  public async Task<ReadOperationResult> GetUpdatesAfterCheckpoint(OperationStateAndConfig<ReadOperationConfig> config) {
    var updates = config.State.Object.Value switch { 
      nameof(CrmMembershipType) => await crm.GetMembershipTypes(config.Checkpoint), 
      nameof(CrmCustomer) => await crm.GetCustomers(config.Checkpoint), 
      nameof(CrmInvoice) => await crm.GetInvoices(config.Checkpoint), 
      _ => throw new NotSupportedException(config.State.Object) 
    };
    if (updates.Any()) ctx.Debug($"CrmReadFunction[{config.State.Object.Value}] Updates[{updates.Count}]\n\t" + String.Join("\n\t", updates));
    return ReadOperationResult.Create(updates);
  }
}

public class CrmPromoteFunction : AbstractFunction<PromoteOperationConfig, PromoteOperationResult>, IEvaluateEntitiesToPromote {
  
  public override FunctionConfig<PromoteOperationConfig> Config { get; }
  
  private readonly SimulationCtx ctx;
  private readonly FunctionHelpers help;

  public CrmPromoteFunction(SimulationCtx ctx) {
    this.ctx = ctx;
    help = new FunctionHelpers(SimulationConstants.CRM_SYSTEM, ctx.checksum, ctx.entitymap); 
    Config = new(SimulationConstants.CRM_SYSTEM, LifecycleStage.Defaults.Promote, [
      new(new(nameof(CrmMembershipType)), CoreEntityType.From<CoreMembershipType>(), TestingDefaults.CRON_EVERY_SECOND, this),
      new(new(nameof(CrmCustomer)), CoreEntityType.From<CoreCustomer>(), TestingDefaults.CRON_EVERY_SECOND, this) { IsBidirectional = ctx.ALLOW_BIDIRECTIONAL },
      new(new(nameof(CrmInvoice)), CoreEntityType.From<CoreInvoice>(), TestingDefaults.CRON_EVERY_SECOND, this) { IsBidirectional = ctx.ALLOW_BIDIRECTIONAL }
    ]);
  }

  public async Task<PromoteOperationResult> Evaluate(OperationStateAndConfig<PromoteOperationConfig> config, List<StagedEntity> staged) {
    ctx.Debug($"CrmPromoteFunction[{config.State.Object.Value}] Staged[{staged.Count}]");
    var topromote = config.State.Object.Value switch { 
      nameof(CoreMembershipType) => staged.ToStagedSysCore<CrmMembershipType>(ctx.CrmMembershipTypeToCoreMembershipType), 
      nameof(CoreCustomer) => await EvaluateCustomers(), 
      nameof(CoreInvoice) => await EvaluateInvoices(), 
      _ => throw new NotSupportedException(config.State.Object) };
    return new SuccessPromoteOperationResult(topromote, []);

    async Task<List<Containers.StagedSysCore>> EvaluateCustomers() {
      var map = await help.GetRelatedEntityCoreIdsFromSystemIds(staged.ToSysEnt<CrmCustomer>(), "SystemId", CoreEntityType.From<CoreCustomer>(), false);
      return staged.ToStagedSysCore<CrmCustomer>(c => {
        var existing = map.TryGetValue(c.SystemId, out var coreid) ? ctx.core.GetCustomer(coreid) : null;
        return ctx.CrmCustomerToCoreCustomer(c, existing);
      });
    }
    
    async Task<List<Containers.StagedSysCore>> EvaluateInvoices() {
      var maps = await help.GetRelatedEntityCoreIdsFromSystemIds(staged.ToSysEnt<CrmInvoice>(), nameof(CrmInvoice.CustomerId), CoreEntityType.From<CoreCustomer>(), true);
      return staged.ToStagedSysCore<CrmInvoice>(e => ctx.CrmInvoiceToCoreInvoice(e, maps[e.CustomerId.ToString()]));
    }
  }
}

public class CrmWriteFunction : AbstractFunction<WriteOperationConfig, WriteOperationResult>, ITargetSystemWriter {
  
  public override FunctionConfig<WriteOperationConfig> Config { get; }
  
  private readonly SimulationCtx ctx;
  private readonly CrmSystem crm;
  private readonly FunctionHelpers help; 

  public CrmWriteFunction(SimulationCtx ctx, CrmSystem crm) {
    this.ctx = ctx;
    this.crm = crm;
    help = new(SimulationConstants.CRM_SYSTEM, ctx.checksum, ctx.entitymap);
    Config = new(SimulationConstants.CRM_SYSTEM, LifecycleStage.Defaults.Write, [
      new(CoreEntityType.From<CoreCustomer>(), TestingDefaults.CRON_EVERY_SECOND, this),
      new(CoreEntityType.From<CoreInvoice>(), TestingDefaults.CRON_EVERY_SECOND, this)
    ]);
  }

  public async Task<(List<CoreSystemAndPendingCreateMap>, List<CoreSystemAndPendingUpdateMap>)> CovertCoreEntitiesToSystemEntitties(WriteOperationConfig config, List<CoreAndPendingCreateMap> tocreate, List<CoreAndPendingUpdateMap> toupdate) {
    ctx.Debug($"CrmWriteFunction.CovertCoreEntitiesToSystemEntitties[{config.Object.Value}] Create[{tocreate.Count}] Updated[{toupdate.Count}]");
    if (config.Object.Value == nameof(CoreCustomer)) {
      return help.CovertCoreEntitiesToSystemEntitties<CoreCustomer>(tocreate, toupdate, (id, e) => FromCore(Id(id), e));
    }
    if (config.Object.Value == nameof(CoreInvoice)) {
      var cores = tocreate.ToCore().Concat(toupdate.ToCore()).ToList();
      var maps = await help.GetRelatedEntitySystemIdsFromCoreIds(cores, nameof(CoreInvoice.CustomerId), CoreEntityType.From<CoreCustomer>());
      return help.CovertCoreEntitiesToSystemEntitties<CoreInvoice>(tocreate, toupdate, (id, e) => FromCore(Id(id), e, maps));
    }
    throw new NotSupportedException(config.Object);
    
    Guid Id(string id) => id == String.Empty ? Guid.Empty : Guid.Parse(id);
  }

  public async Task<WriteOperationResult> WriteEntitiesToTargetSystem(WriteOperationConfig config, List<CoreSystemAndPendingCreateMap> tocreate, List<CoreSystemAndPendingUpdateMap> toupdate) {
    ctx.Debug($"CrmWriteFunction.WriteEntitiesToTargetSystem[{config.Object.Value}] Created[{tocreate.Count}] Updated[{toupdate.Count}]");
    if (config.Object.Value == nameof(CoreCustomer)) {
      var (created, updated) = (await crm.CreateCustomers(tocreate.ToSysEnt<CrmCustomer>()), await crm.UpdateCustomers(toupdate.ToSysEnt<CrmCustomer>()));
      return new SuccessWriteOperationResult(tocreate.SuccessCreate(created, ctx.checksum), toupdate.SuccessUpdate(updated, ctx.checksum));
    }
    if (config.Object.Value == nameof(CoreInvoice)) {
      var (created, updated) = (await crm.CreateInvoices(tocreate.ToSysEnt<CrmInvoice>()), await crm.UpdateInvoices(toupdate.ToSysEnt<CrmInvoice>()));
      return new SuccessWriteOperationResult(tocreate.SuccessCreate(created, ctx.checksum), toupdate.SuccessUpdate(updated, ctx.checksum));
    }
    throw new NotSupportedException(config.Object);
  }
  
  private CrmCustomer FromCore(Guid id, CoreCustomer c) => new(id, UtcDate.UtcNow, Guid.Parse(c.MembershipId), c.Name);

  private CrmInvoice FromCore(Guid id, CoreInvoice i, Dictionary<ValidString, ValidString> custmaps) => 
      new(id, UtcDate.UtcNow, Guid.Parse(custmaps[i.CustomerId]), i.Cents, i.DueDate, i.PaidDate);

}