using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.CoreToSystemMapping;
using Centazio.Core.Misc;
using Centazio.Core.Promote;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Core.Stage;
using Centazio.Core.Write;
using Centazio.E2E.Tests.Infra;
using Centazio.E2E.Tests.Systems;
using Centazio.E2E.Tests.Systems.Crm;
using Centazio.E2E.Tests.Systems.Fin;
using Centazio.Test.Lib;
using Serilog;
using Serilog.Events;

namespace Centazio.E2E.Tests;

[IgnoreNamingConventions]
public static class SimulationConstants {
  public static readonly SystemName CRM_SYSTEM = new(nameof(CrmSystem));
  public static readonly SystemName FIN_SYSTEM = new(nameof(FinSystem));
}

public class EpochTracker(int epoch, SimulationCtx ctx) {
  public int Epoch { get; } = epoch;
  
  private readonly Dictionary<(Type, string), ICoreEntity> added = new();
  private readonly Dictionary<(Type, string), ICoreEntity> updated = new();
  
  public async Task ValidateAdded<T>(params IEnumerable<ISystemEntity>[] expected) where T : ICoreEntity {
    var ascore = await SysEntsToCore(expected);
    var actual = added.Values.Where(e => e.GetType() == typeof(T)).ToList();
    Assert.That(actual.Count, Is.EqualTo(ascore.Count), $"Expected {typeof(T).Name} Created({ascore.Count}):\n\t" + String.Join("\n\t", ascore.Select(e => $"{e.DisplayName}({e.CoreId})")) + 
        $"\nActual {typeof(T).Name} Created({actual.Count}):\n\t" + String.Join("\n\t", actual.Select(e => $"{e.DisplayName}({e.CoreId})")));
    
    Assert.That(actual.All(e => e.DateUpdated == UtcDate.UtcNow));
    Assert.That(actual.All(e => e.DateCreated == UtcDate.UtcNow));
  }

  public async Task ValidateUpdated<T>(params IEnumerable<ISystemEntity>[] expected) where T : ICoreEntity {
    var eadded = added.Values.Where(e => e.GetType() == typeof(T)).ToList();
    var ascore = (await SysEntsToCore(expected))
        .DistinctBy(e => (e.GetType(), e.CoreId))
        // remove from validation if these entities were added in this epoch, as
        // they will be validated in the `ValidateAdded` method
        .Where(e => !eadded.Contains(e)) 
        .ToList();
    var actual = updated.Values.Where(e => e.GetType() == typeof(T)).ToList();
    Assert.That(actual.Count, Is.EqualTo(ascore.Count), $"Expected {typeof(T).Name} Updated({ascore.Count}):\n\t" + String.Join("\n\t", ascore.Select(e => $"{e.DisplayName}({e.CoreId})")) + 
        $"\nActual {typeof(T).Name} Updated({actual.Count}):\n\t" + String.Join("\n\t", actual.Select(e => $"{e.DisplayName}({e.CoreId})")));
    Assert.That(actual.All(e => e.DateUpdated == UtcDate.UtcNow));
    Assert.That(actual.All(e => e.DateCreated < UtcDate.UtcNow));
  }
  
  private async Task<List<ICoreEntity>> SysEntsToCore(params IEnumerable<ISystemEntity>[] expected) {
    var cores = new List<ICoreEntity>();
    var allsums = new Dictionary<string, bool>();
    foreach (var sysent in expected) {
      var syscores = await sysent.Select(ToCore).Synchronous();
      var sums = syscores.Select(c => ctx.checksum.Checksum(c)).Distinct().ToList();
      if (syscores.Count != sums.Count) throw new Exception($"Expected all core entities from a system to be unique.  Found some entities that resulted in the same ICoreEntity checksum");
      syscores.ForEach((c, idx) => {
        if (allsums.ContainsKey(sums[idx])) return;
        
        allsums.Add(sums[idx], true);
        cores.Add(c);
      });
    }
    return cores;

    Task<ICoreEntity> ToCore(ISystemEntity e) {
      var exid = ctx.entitymap.Db.Keys.SingleOrDefault(m => m.SystemId == e.SystemId)?.CoreId;
      ICoreEntity result = e switch {
        CrmMembershipType type => ctx.CrmMembershipTypeToCoreMembershipType(type, ctx.core.GetMembershipType(exid)), 
        CrmCustomer customer => ctx.CrmCustomerToCoreCustomer(customer, ctx.core.GetCustomer(exid)), 
        CrmInvoice invoice => ctx.CrmInvoiceToCoreInvoice(invoice, ctx.core.GetInvoice(exid)), 
        FinAccount account => ctx.FinAccountToCoreCustomer(account, ctx.core.GetCustomer(exid)), 
        FinInvoice fininv => ctx.FinInvoiceToCoreInvoice(fininv, ctx.core.GetInvoice(exid)), 
        _ => throw new NotSupportedException(e.GetType().Name)
      };
      return Task.FromResult(result);
    }
  }

  public void Add(ICoreEntity e) {
    if (!added.TryAdd((e.GetType(), e.CoreId), e)) throw new Exception($"entity appears to have already been added: {e}");
    Log.Information($"Validation.Add[{e.DisplayName}({e.CoreId})]");
  }
  
  public void Update(ICoreEntity e) {
    // ignore entities that have already been added in this epoch, they will be validated as part of the added validation
    if (added.ContainsKey((e.GetType(), e.CoreId))) return; 
    updated[(e.GetType(), e.CoreId)] = e;
    Log.Information($"Update.Add[{e.DisplayName}({e.CoreId})]");
  }

}

public class TestingInMemoryCoreToSystemMapStore : InMemoryCoreToSystemMapStore {
  public Dictionary<Map.Key, string> Db => memdb;
}

public class SimulationCtx {
  
  public readonly bool SILENCE_LOGGING = false;
  public readonly bool SILENCE_SIMULATION = false;
  public readonly bool ALLOW_BIDIRECTIONAL = true;
  public List<string> LOGGING_FILTERS { get; } = [];
  
  public readonly Random rng = new(1);
  
  public Guid Guid() { // random but seedable guid 
    var guid = new byte[16];
    rng.NextBytes(guid);
    return new Guid(guid);
  }
  
  private readonly Sha256ChecksumAlgorithm algo = new();
  public readonly ICtlRepository ctl = new InMemoryCtlRepository();
  public readonly TestingInMemoryCoreToSystemMapStore entitymap = new();
  
  public readonly IChecksumAlgorithm checksum;
  
  public EpochTracker Epoch { get; set; }
  public readonly CoreStorage core;
  public readonly IStagedEntityStore stage;
  public readonly FunctionHelpers crmhelp;
  public readonly FunctionHelpers finhelp;

  internal SimulationCtx() {
    checksum = algo;
    core = new(this);
    stage = new InMemoryStagedEntityStore(0, checksum.Checksum);
    crmhelp = new FunctionHelpers(SimulationConstants.CRM_SYSTEM, checksum, entitymap);
    finhelp = new FunctionHelpers(SimulationConstants.FIN_SYSTEM, checksum, entitymap);
    Epoch = new(0, this);
  }

  public readonly int TOTAL_EPOCHS = 250;
  public readonly int CRM_MAX_EDIT_MEMBERSHIPS = 2;
  public readonly int CRM_MAX_NEW_CUSTOMERS = 4;
  public readonly int CRM_MAX_EDIT_CUSTOMERS = 4;
  public readonly int CRM_MAX_NEW_INVOICES = 4;
  public readonly int CRM_MAX_EDIT_INVOICES = 4;
  
  public readonly int FIN_MAX_NEW_ACCOUNTS = 2;
  public readonly int FIN_MAX_EDIT_ACCOUNTS = 4;
  public readonly int FIN_MAX_NEW_INVOICES = 2;
  public readonly int FIN_MAX_EDIT_INVOICES = 2;
 
 public void Debug(string message) {
   if (SILENCE_SIMULATION) return;
   if (LogInitialiser.LevelSwitch.MinimumLevel < LogEventLevel.Fatal) Log.Information(message);
   else DevelDebug.WriteLine(message);
 }
 
 public string NewName<T>(string prefix, List<T> target, int idx) => $"{prefix}_{target.Count + idx}:0";
 public string UpdateName(string name) {
   var (label, count, _) = name.Split(':');
   return $"{label}:{Int32.Parse(count) + 1}";
 }
 
 public readonly Dictionary<SystemEntityId, CoreEntityId> systocoreids = new();
 public readonly Dictionary<CoreEntityId, SystemEntityId> coretosysids = new();
 
 private int ceid;
 public CoreEntityId NewCoreEntityId<T>(SystemName system, SystemEntityId systemid) where T : ICoreEntity {
   var coreid = new CoreEntityId($"{system}/{typeof(T).Name}[{++ceid}]");
   systocoreids[systemid] = coreid;
   coretosysids[coreid] = systemid;
   return coreid;
 }

 public CoreCustomer CrmCustomerToCoreCustomer(CrmCustomer c, CoreCustomer? existing) => 
      existing is null 
          ? new(NewCoreEntityId<CoreCustomer>(SimulationConstants.CRM_SYSTEM, c.SystemId), c.SystemId, c.Name, systocoreids[c.MembershipTypeSystemId])
          : existing with { Name = c.Name, MembershipCoreId = systocoreids[c.MembershipTypeSystemId] };

  public CoreInvoice CrmInvoiceToCoreInvoice(CrmInvoice i, CoreInvoice? existing, CoreEntityId? custcoreid = null) {
    custcoreid ??= entitymap.Db.Keys.Single(m => m.System == SimulationConstants.CRM_SYSTEM && m.CoreEntityType == CoreEntityType.From<CoreCustomer>() && m.SystemId == i.CustomerSystemId).CoreId;
    if (existing is not null && existing.CustomerCoreId != custcoreid) { throw new Exception("trying to change customer on an invoice which is not allowed"); }
    return existing is null 
        ? new CoreInvoice(NewCoreEntityId<CoreInvoice>(SimulationConstants.CRM_SYSTEM, i.SystemId), i.SystemId, custcoreid, i.AmountCents, i.DueDate, i.PaidDate)
        : existing with { Cents = i.AmountCents, DueDate = i.DueDate, PaidDate = i.PaidDate };
  }
  
  public CoreCustomer FinAccountToCoreCustomer(FinAccount a, CoreCustomer? existing) => 
      existing is null 
          ? new CoreCustomer(NewCoreEntityId<CoreCustomer>(SimulationConstants.FIN_SYSTEM, a.SystemId), a.SystemId, a.Name, systocoreids[CrmSystem.PENDING_MEMBERSHIP_TYPE_ID])
          : existing with { Name = a.Name };

  public CoreInvoice FinInvoiceToCoreInvoice(FinInvoice i, CoreInvoice? existing, CoreEntityId? custcoreid = null) {
    custcoreid ??= entitymap.Db.Keys.Single(m => m.System == SimulationConstants.FIN_SYSTEM && m.CoreEntityType == CoreEntityType.From<CoreCustomer>() && m.SystemId == i.AccountSystemId).CoreId;
    if (existing is not null && existing.CustomerCoreId != custcoreid) { throw new Exception("trying to change customer on an invoice which is not allowed"); }
    return existing is null 
        ? new CoreInvoice(NewCoreEntityId<CoreInvoice>(SimulationConstants.FIN_SYSTEM, i.SystemId), i.SystemId, custcoreid, (int)(i.Amount * 100), DateOnly.FromDateTime(i.DueDate), i.PaidDate) 
        : existing with { Cents = (int)(i.Amount * 100), DueDate = DateOnly.FromDateTime(i.DueDate), PaidDate = i.PaidDate };
  }

  public CoreMembershipType CrmMembershipTypeToCoreMembershipType(CrmMembershipType m, CoreMembershipType? existing) => 
      existing is null 
        ? new(NewCoreEntityId<CoreMembershipType>(SimulationConstants.CRM_SYSTEM, m.SystemId), m.SystemId, m.Name)
        : existing with { Name = m.Name };

  public CrmMembershipType CoreMembershipTypeToCrmMembershipType(Guid id, CoreMembershipType m) => new(id, UtcDate.UtcNow, m.Name);
  public CrmCustomer CoreCustomerToCrmCustomer(Guid id, CoreCustomer c) => new(id, UtcDate.UtcNow, System.Guid.Parse(coretosysids[c.MembershipCoreId].Value), c.Name);

  public CrmInvoice CoreInvoiceToCrmInvoice(Guid id, CoreInvoice i, Dictionary<CoreEntityId, SystemEntityId> custmaps) => 
      new(id, UtcDate.UtcNow, System.Guid.Parse(custmaps[i.CustomerCoreId].Value), i.Cents, i.DueDate, i.PaidDate);
  
  public FinAccount CoreCustomerToFinAccount(int id, CoreCustomer c) => new(id, c.Name, UtcDate.UtcNow);
  
  public FinInvoice CoreInvoiceToFinInvoice(int id, CoreInvoice i, Dictionary<CoreEntityId, SystemEntityId> accmaps) => 
      new(id, Int32.Parse(accmaps[i.CustomerCoreId]), i.Cents / 100.0m, UtcDate.UtcNow, i.DueDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), i.PaidDate);

  public List<T> ShuffleAndTake<T>(IEnumerable<T> enumerable, int? take = null) {
    var list = enumerable.ToList();
    var n = list.Count;
    while (n > 1) {
      n--;
      var k = rng.Next(n + 1);
      (list[k], list[n]) = (list[n], list[k]);
    }
    return take.HasValue ? list.Take(take.Value).ToList() : list;
  }
  
  public T RandomItem<T>(IList<T> lst) => lst[rng.Next(lst.Count)];
}

public class E2EEnvironment : IAsyncDisposable {

  private static readonly SimulationCtx ctx = new();
  
  // Crm
  private readonly CrmSystem crm = new(ctx);
  private readonly FunctionRunner<ReadOperationConfig, ReadOperationResult> crm_read_runner;
  private readonly FunctionRunner<PromoteOperationConfig, PromoteOperationResult> crm_promote_runner;
  private readonly FunctionRunner<WriteOperationConfig, WriteOperationResult> crm_write_runner;

  // Fin
  private readonly FinSystem fin = new(ctx);
  private readonly FunctionRunner<ReadOperationConfig, ReadOperationResult> fin_read_runner;
  private readonly FunctionRunner<PromoteOperationConfig, PromoteOperationResult> fin_promote_runner;
  private readonly FunctionRunner<WriteOperationConfig, WriteOperationResult> fin_write_runner;

  public E2EEnvironment() {
    crm_read_runner = new FunctionRunner<ReadOperationConfig, ReadOperationResult>(new CrmReadFunction(ctx, crm),
        new ReadOperationRunner(ctx.stage),
        ctx.ctl);
    crm_promote_runner = new FunctionRunner<PromoteOperationConfig, PromoteOperationResult>(new CrmPromoteFunction(ctx),
        new PromoteOperationRunner(ctx.stage, ctx.core, ctx.entitymap),
        ctx.ctl);
    
    crm_write_runner = new FunctionRunner<WriteOperationConfig, WriteOperationResult>(new CrmWriteFunction(ctx, crm),
        new WriteOperationRunner<WriteOperationConfig>(ctx.entitymap, ctx.core), 
        ctx.ctl);
    
    fin_read_runner = new FunctionRunner<ReadOperationConfig, ReadOperationResult>(new FinReadFunction(ctx, fin),
        new ReadOperationRunner(ctx.stage),
        ctx.ctl);
    fin_promote_runner = new FunctionRunner<PromoteOperationConfig, PromoteOperationResult>(new FinPromoteFunction(ctx),
        new PromoteOperationRunner(ctx.stage, ctx.core, ctx.entitymap),
        ctx.ctl);
    
    fin_write_runner = new FunctionRunner<WriteOperationConfig, WriteOperationResult>(new FinWriteFunction(ctx, fin),
        new WriteOperationRunner<WriteOperationConfig>(ctx.entitymap, ctx.core), 
        ctx.ctl);
  }

  public async ValueTask DisposeAsync() {
    await ctx.stage.DisposeAsync();
    await ctx.ctl.DisposeAsync();
  }

  [Test] public async Task RunSimulation() {
    if (ctx.SILENCE_LOGGING) LogInitialiser.LevelSwitch.MinimumLevel = LogEventLevel.Fatal;
    if (ctx.LOGGING_FILTERS.Any()) {
      Log.Logger = LogInitialiser.GetConsoleConfig(filters: ctx.LOGGING_FILTERS).CreateLogger();
      Log.Information($"Logging Filter Enabled[{String.Join(',', ctx.LOGGING_FILTERS)}]");
    }
    
    await Enumerable.Range(0, ctx.TOTAL_EPOCHS).Select(RunEpoch).Synchronous();
  }

  private async Task RunEpoch(int epoch) {
    ctx.Epoch = new(epoch, ctx);
    RandomTimeStep();
    ctx.Debug($"Epoch[{epoch}] Starting {{{UtcDate.UtcNow:o}}}");
    
    crm.Simulation.Step();
    fin.Simulation.Step(); 
    
    ctx.Debug($"Epoch[{epoch}] Simulation Step Completed - Running Functions");
    
    await RunFunc(crm_read_runner);
    await RunFunc(crm_promote_runner);
    await RunFunc(fin_read_runner); 
    await RunFunc(fin_promote_runner);
    await RunFunc(crm_write_runner);
    await RunFunc(fin_write_runner);
    
    ctx.Debug($"Epoch[{epoch}] Functions Completed - Validating");
    await ValidateEpoch();
  }
  
  private Task<FunctionRunResults<R>> RunFunc<C, R>(FunctionRunner<C, R> runner) 
      where C : OperationConfig 
      where R : OperationResult {
    ctx.Debug($"Running[{runner.System}/{runner.Stage}] Now[{UtcDate.UtcNow:o}]");
    return runner.RunFunction(); 
  }

  private void RandomTimeStep() {
    TestingUtcDate.DoTick(new TimeSpan(ctx.rng.Next(0, 2), ctx.rng.Next(0, 24), ctx.rng.Next(0, 60), ctx.rng.Next(0, 60)));
  }
  
  private async Task ValidateEpoch() {
    await CompareMembershipTypes();
    await CompareCustomers();
    await CompareInvoices();
  }

  private async Task CompareMembershipTypes() {
    var core_types = ctx.core.GetMembershipTypes().Select(m => ctx.CoreMembershipTypeToCrmMembershipType(Guid.Empty, m));
    
    await ctx.Epoch.ValidateAdded<CoreMembershipType>(ctx.Epoch.Epoch == 0 ? crm.MembershipTypes : []);
    await ctx.Epoch.ValidateUpdated<CoreMembershipType>(crm.Simulation.EditedMemberships);
    CompareByChecksum(SimulationConstants.CRM_SYSTEM, core_types, crm.MembershipTypes);
  }
  
  private async Task CompareCustomers() {
    var core_customers_for_crm = ctx.core.GetCustomers().Select(c => ctx.CoreCustomerToCrmCustomer(Guid.Empty, c));
    var core_customers_for_fin = ctx.core.GetCustomers().Select(c => ctx.CoreCustomerToFinAccount(0, c));
    
    await ctx.Epoch.ValidateAdded<CoreCustomer>(crm.Simulation.AddedCustomers, fin.Simulation.AddedAccounts);
    await ctx.Epoch.ValidateUpdated<CoreCustomer>(crm.Simulation.EditedCustomers, fin.Simulation.EditedAccounts);
    CompareByChecksum(SimulationConstants.CRM_SYSTEM, core_customers_for_crm, crm.Customers);
    CompareByChecksum(SimulationConstants.FIN_SYSTEM, core_customers_for_fin, fin.Accounts);
  }
  
  private async Task CompareInvoices() {
    var cores = ctx.core.GetInvoices();
    var crmmaps = await ctx.crmhelp.GetRelatedEntitySystemIdsFromCoreIds(CoreEntityType.From<CoreCustomer>(), cores.Cast<ICoreEntity>().ToList(), nameof(CoreInvoice.CustomerCoreId));
    var finmaps = await ctx.finhelp.GetRelatedEntitySystemIdsFromCoreIds(CoreEntityType.From<CoreCustomer>(), cores.Cast<ICoreEntity>().ToList(), nameof(CoreInvoice.CustomerCoreId));
    var core_invoices_for_crm = cores.Select(i => ctx.CoreInvoiceToCrmInvoice(Guid.Empty, i, crmmaps));
    var core_invoices_for_fin = cores.Select(i => ctx.CoreInvoiceToFinInvoice(0, i, finmaps));
    
    await ctx.Epoch.ValidateAdded<CoreInvoice>(crm.Simulation.AddedInvoices, fin.Simulation.AddedInvoices);
    await ctx.Epoch.ValidateUpdated<CoreInvoice>(crm.Simulation.EditedInvoices, fin.Simulation.EditedInvoices);
    CompareByChecksum(SimulationConstants.CRM_SYSTEM, core_invoices_for_crm, crm.Invoices);
    CompareByChecksum(SimulationConstants.FIN_SYSTEM, core_invoices_for_fin, fin.Invoices);
  }
  
  private void CompareByChecksum(SystemName system, IEnumerable<ISystemEntity> cores, IEnumerable<ISystemEntity> targets) {
    var (corecs, targetscs) = (cores.Select(c => Json.Serialize(c.GetChecksumSubset())).ToList(), targets.Select(t => Json.Serialize(t.GetChecksumSubset())).ToList());
    Assert.That(targetscs, Is.EquivalentTo(corecs), $"[{system}] checksum comparison failed\ncore entities:\n\t{String.Join("\n\t", corecs)}\ntarget system entities:\n\t{String.Join("\n\t", targetscs)}");
  }
}