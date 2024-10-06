using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.EntitySysMapping;
using Centazio.Core.Promote;
using Centazio.Core.Read;
using Centazio.Core.Runner;
using Centazio.Core.Stage;
using Centazio.Core.Write;
using Centazio.E2E.Tests.Infra;
using Centazio.E2E.Tests.Systems.Crm;
using Centazio.E2E.Tests.Systems.Fin;
using Centazio.Test.Lib;
using Serilog;
using Serilog.Events;

namespace Centazio.E2E.Tests;

public static class SimulationConstants {
  public static readonly SystemName CRM_SYSTEM = new(nameof(CrmSystem));
  public static readonly SystemName FIN_SYSTEM = new(nameof(FinSystem));
}

public class EpochTracker(int epoch, SimulationCtx ctx) {
  public int Epoch { get; } = epoch;
  
  private readonly Dictionary<(Type, string), ICoreEntity> added = new();
  private readonly Dictionary<(SystemName, Type, string), ICoreEntity> updated = new();
  
  public async Task ValidateAdded<T>(params IEnumerable<ISystemEntity>[] expected) where T : ICoreEntity {
    var ascore = await ExternalEntitiesToCore(expected);
    var actual = added.Values.Where(e => e.GetType() == typeof(T)).ToList();
    Assert.That(actual.Count, Is.EqualTo(ascore.Count), $"ValidateAdded Type[{typeof(T).Name}] Expected[{ascore.Count()}] Actual[{actual.Count}]" +
        $"\nExpected Items:\n\t" + String.Join("\n\t", ascore.Select(e => $"{e.DisplayName}({e.Id})")) + 
        "\nActual Items:\n\t" + String.Join("\n\t", actual.Select(e => $"{e.DisplayName}({e.Id})")));
  }

  public async Task ValidateUpdated<T>(params IEnumerable<ISystemEntity>[] expected) where T : ICoreEntity {
    var ascore = await ExternalEntitiesToCore(expected);
    var actual = updated.Values.Where(e => e.GetType() == typeof(T)).ToList();
    Assert.That(actual.Count, Is.EqualTo(ascore.Count), $"ValidateUpdated Type[{typeof(T).Name}] Expected[{ascore.Count}] Actual[{actual.Count}]" +
        $"\nExpected Items:\n\t" + String.Join("\n\t", ascore.Select(e => $"{e.DisplayName}({e.Id})")) + 
        "\nActual Items:\n\t" + String.Join("\n\t", actual.Select(e => $"{e.DisplayName}({e.Id})")));
  }
  
  private async Task<List<ICoreEntity>> ExternalEntitiesToCore(params IEnumerable<ISystemEntity>[] expected) {
    var cores = new List<ICoreEntity>();
    var allsums = new Dictionary<string, bool>();
    foreach (var externals in expected) {
      var syscores = await externals.Select(ToCore).Synchronous();
      var sums = syscores.Select(c => ctx.checksum.Checksum(c)).Distinct().ToList();
      if (syscores.Count != sums.Count) throw new Exception($"Expected all core entities from an external system to be unique.  Found some external entities that resulted in the same ICoreEntity checksum");
      syscores.ForEach((c, idx) => {
        if (allsums.ContainsKey(sums[idx])) return;
        
        allsums.Add(sums[idx], true);
        cores.Add(c);
      });
    }
    return cores;

    async Task<ICoreEntity> ToCore(object e) => e switch { 
      CrmMembershipType type => ctx.CrmMembershipTypeToCoreMembershipType(type), 
      CrmCustomer customer => ctx.CrmCustomerToCoreCustomer(customer), 
      CrmInvoice invoice => ctx.CrmInvoiceToCoreInvoice(invoice), 
      FinAccount account => await FromFinAccount(account), 
      FinInvoice finInvoice => ctx.FinInvoiceToCoreInvoice(finInvoice), 
      _ => throw new NotSupportedException(e.GetType().Name) };

    async Task<CoreCustomer> FromFinAccount(FinAccount account) {
      var externalid = account.SystemId;
      var map = (await ctx.entitymap.GetExistingMappingsFromExternalIds(CoreEntityType.From<CoreCustomer>(), [externalid], SimulationConstants.FIN_SYSTEM)).Single();
      var coreid = map.CoreId;
      var existing = ctx.core.GetCustomer(coreid); 
      return ctx.FinAccountToCoreCustomer(account, existing);
    }
  }

  public void Add(ICoreEntity e) {
    ctx.Debug($"CoreStorage.Add CoreEntityType[{e.GetType().Name}] Name[{e.DisplayName}] Id[{e.Id}]");
    if (!added.TryAdd((e.GetType(), e.Id), e)) throw new Exception($"entity appears to have already been added: {e}");
  }
  public void Update(SystemName system, ICoreEntity e) {
    ctx.Debug($"CoreStorage.Update System[{system}] CoreEntityType[{e.GetType().Name}] Name[{e.DisplayName}] Id[{e.Id}]");
    updated[(system, e.GetType(), e.Id)] = e;
  }

}

public class TestingInMemoryCoreToSystemMapStore : InMemoryCoreToSystemMapStore {
  public List<CoreToSystemMap> Db => memdb.Values.ToList();
}

public class SimulationCtx {
  
  public readonly bool SILENCE_LOGGING = false;
  public readonly bool SILENCE_SIMULATION = false;
  public readonly bool ALLOW_BIDIRECTIONAL = true;
  
  public readonly Random rng = new(1);
  private readonly Sha256ChecksumAlgorithm algo = new();
  public readonly ICtlRepository ctl = new InMemoryCtlRepository();
  public readonly TestingInMemoryCoreToSystemMapStore entitymap = new();
  
  public readonly IChecksumAlgorithm checksum;
  
  public EpochTracker Epoch { get; set; }
  public readonly CoreStorage core;
  public readonly IStagedEntityStore stage;
  public SystemName CurrentSystem { get; set; } = null!;

  internal SimulationCtx() {
    checksum = algo;
    core = new(this);
    stage = new InMemoryStagedEntityStore(0, checksum.Checksum);
    Epoch = new(0, this);
  }

  public readonly int TOTAL_EPOCHS = 250;
  public readonly int CRM_MAX_EDIT_MEMBERSHIPS = 2;
  public readonly int CRM_MAX_NEW_CUSTOMERS = 4;
  public readonly int CRM_MAX_EDIT_CUSTOMERS = 4;
  public readonly int CRM_MAX_NEW_INVOICES = 4;
  public readonly int CRM_MAX_EDIT_INVOICES = 4;
  
  // todo: adding this causes issues
  public readonly int FIN_MAX_NEW_ACCOUNTS = 2;
  public readonly int FIN_MAX_EDIT_ACCOUNTS = 4;
  public readonly int FIN_MAX_NEW_INVOICES = 0;
  public readonly int FIN_MAX_EDIT_INVOICES = 0;
 
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
 
  public CoreCustomer CrmCustomerToCoreCustomer(CrmCustomer c) {
    var (membership, invoices) = (core.GetMembershipType(c.MembershipTypeId.ToString()), core.GetInvoicesForCustomer(c.SystemId));
    return new CoreCustomer(c.SystemId, SimulationConstants.CRM_SYSTEM, c.Updated, c.Name, membership, invoices);
  }
  
  // todo: this `CoreCustomer? existing` is required in the promote function as promote should
  // fill in details on this target instead of creating new entity
  public CoreCustomer FinAccountToCoreCustomer(FinAccount a, CoreCustomer? existing) {
    if (existing is not null) return existing with { SourceSystemDateUpdated = a.Updated, Name = a.Name };
    var membership = core.GetMembershipType(CrmSystem.PENDING_MEMBERSHIP_TYPE_ID.ToString());
    return new CoreCustomer(a.SystemId, SimulationConstants.FIN_SYSTEM, a.Updated, a.Name, membership, []);
  }
  public CoreInvoice CrmInvoiceToCoreInvoice(CrmInvoice i, string? custid = null) {
    // todo: add in if statement back after validating works
    var newcustid = entitymap.Db.Single(m => m.System == SimulationConstants.CRM_SYSTEM && m.CoreEntity == CoreEntityType.From<CoreCustomer>() && m.ExternalId == i.CustomerId.ToString()).CoreId;
    if (custid is not null && newcustid != custid) throw new Exception();
      
    return new CoreInvoice(i.SystemId, SimulationConstants.CRM_SYSTEM, i.Updated, custid ?? newcustid, i.AmountCents, i.DueDate, i.PaidDate);
  }

  public CoreInvoice FinInvoiceToCoreInvoice(FinInvoice i, string? custid = null) {
    // todo: add in if statement back after validating works
    var newcustid = entitymap.Db.Single(m => m.System == SimulationConstants.FIN_SYSTEM && m.CoreEntity == CoreEntityType.From<CoreCustomer>() && m.ExternalId == i.AccountId.ToString()).CoreId;
    if (custid is not null && newcustid != custid) throw new Exception();
    return new CoreInvoice(i.SystemId, SimulationConstants.FIN_SYSTEM, i.Updated, custid ?? newcustid, (int)(i.Amount * 100), DateOnly.FromDateTime(i.DueDate), i.PaidDate);
  }

  public CoreMembershipType CrmMembershipTypeToCoreMembershipType(CrmMembershipType m) => new(m.SystemId, m.Updated, m.Name);
  
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
    
    crm_write_runner = new FunctionRunner<WriteOperationConfig, WriteOperationResult>(new CrmWriteFunction(ctx, crm, ctx.entitymap),
        new WriteOperationRunner<WriteOperationConfig>(ctx.entitymap, ctx.core), 
        ctx.ctl);
    
    fin_read_runner = new FunctionRunner<ReadOperationConfig, ReadOperationResult>(new FinReadFunction(ctx, fin),
        new ReadOperationRunner(ctx.stage),
        ctx.ctl);
    fin_promote_runner = new FunctionRunner<PromoteOperationConfig, PromoteOperationResult>(new FinPromoteFunction(ctx),
        new PromoteOperationRunner(ctx.stage, ctx.core, ctx.entitymap),
        ctx.ctl);
    
    fin_write_runner = new FunctionRunner<WriteOperationConfig, WriteOperationResult>(new FinWriteFunction(ctx, fin, ctx.entitymap),
        new WriteOperationRunner<WriteOperationConfig>(ctx.entitymap, ctx.core), 
        ctx.ctl);
  }

  public async ValueTask DisposeAsync() {
    await ctx.stage.DisposeAsync();
    await ctx.ctl.DisposeAsync();
  }

  [Test] public async Task RunSimulation() {
    if (ctx.SILENCE_LOGGING)  LogInitialiser.LevelSwitch.MinimumLevel = LogEventLevel.Fatal;
    
    await Enumerable.Range(0, ctx.TOTAL_EPOCHS).Select(RunEpoch).Synchronous();
  }

  private async Task RunEpoch(int epoch) {
    ctx.Debug($"Epoch[{epoch}] Starting {{{UtcDate.UtcNow:o}}}");
    ctx.Epoch = new(epoch, ctx);
    
    RandomTimeStep();
    
    ctx.Debug($"Epoch[{epoch}] Simulation Step Completed - Running Functions {{{UtcDate.UtcNow:o}}}");
    
    crm.Simulation.Step();
    fin.Simulation.Step(); // todo: can this be put up with crm?
    
    // todo: these ctx.CurrentSystem = SimulationConstants.CRM_SYSTEM calls are ugly 
    ctx.CurrentSystem = SimulationConstants.CRM_SYSTEM;
    await crm_read_runner.RunFunction();
    await crm_promote_runner.RunFunction();
    
    ctx.CurrentSystem = SimulationConstants.FIN_SYSTEM;
    await fin_read_runner.RunFunction(); 
    await fin_promote_runner.RunFunction();
    
    ctx.CurrentSystem = SimulationConstants.CRM_SYSTEM;
    await crm_write_runner.RunFunction();
    ctx.CurrentSystem = SimulationConstants.FIN_SYSTEM; 
    await fin_write_runner.RunFunction();
    
    // todo: is this final read/promote required
    await crm_read_runner.RunFunction();
    await crm_promote_runner.RunFunction();
    await fin_write_runner.RunFunction(); // todo: is this required?
    
    ctx.Debug($"Epoch[{epoch}] Functions Completed - Validating {{{UtcDate.UtcNow:o}}}");
    await ValidateEpoch();
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
    var core_types = ctx.core.Types.Cast<CoreMembershipType>().Select(m => new { m.Id, m.Name });
    var crm_types = crm.MembershipTypes.Select(m => new { Id = m.SystemId, m.Name } );
    
    await ctx.Epoch.ValidateAdded<CoreMembershipType>(ctx.Epoch.Epoch == 0 ? crm.MembershipTypes : []);
    await ctx.Epoch.ValidateUpdated<CoreMembershipType>(crm.Simulation.EditedMemberships);
    CompareByChecksum(SimulationConstants.CRM_SYSTEM, core_types, crm_types);
  }
  
  private async Task CompareCustomers() {
    var core_customers_for_crm = ctx.core.Customers.Cast<CoreCustomer>().Select(c => new { c.Id, c.Name, MembershipTypeId = c.Membership.Id });
    var core_customers_for_fin = ctx.core.Customers.Cast<CoreCustomer>().Select(c => new { c.Id, c.Name });
    var crm_customers = crm.Customers.Select(c => new { Id = c.SystemId, c.Name, c.MembershipTypeId });
    var fin_accounts = fin.Accounts.Select(c => new { Id = c.SystemId, c.Name });
    
    await ctx.Epoch.ValidateAdded<CoreCustomer>(crm.Simulation.AddedCustomers, fin.Simulation.AddedAccounts);
    await ctx.Epoch.ValidateUpdated<CoreCustomer>(crm.Simulation.EditedCustomers, fin.Simulation.EditedAccounts);
    CompareByChecksum(SimulationConstants.CRM_SYSTEM, core_customers_for_crm, crm_customers);
    CompareByChecksum(SimulationConstants.FIN_SYSTEM, core_customers_for_fin, fin_accounts);
  }
  
  private async Task CompareInvoices() {
    var core_invoices = ctx.core.Invoices.Cast<CoreInvoice>().Select(i => new { i.Id, i.PaidDate, i.DueDate, Amount = i.Cents }).ToList();
    var crm_invoices = crm.Invoices.Select(i => new { Id = i.SystemId, i.PaidDate, i.DueDate, Amount = i.AmountCents });
    var fin_invoices = fin.Invoices.Select(i => new { Id = i.SystemId, i.PaidDate, DueDate = DateOnly.FromDateTime(i.DueDate), Amount = (int) (i.Amount * 100m) });
    
    await ctx.Epoch.ValidateAdded<CoreInvoice>(crm.Simulation.AddedInvoices, fin.Simulation.AddedInvoices);
    await ctx.Epoch.ValidateUpdated<CoreInvoice>(crm.Simulation.EditedInvoices, fin.Simulation.EditedInvoices);
    CompareByChecksum(SimulationConstants.CRM_SYSTEM, core_invoices, crm_invoices);
    CompareByChecksum(SimulationConstants.FIN_SYSTEM, core_invoices, fin_invoices);
  }
  
  private readonly JsonSerializerOptions withid = new();
  private readonly JsonSerializerOptions noid = new() {
    TypeInfoResolver = new DefaultJsonTypeInfoResolver {
      Modifiers = {
        ti => {
          if (ti.Kind != JsonTypeInfoKind.Object) return;
          ti.Properties.Remove(ti.Properties.Single(p => p.Name == "Id"));
        }
      }
    }
  };
  
  private void CompareByChecksum(SystemName targetsys, IEnumerable<object> cores, IEnumerable<object> targets) {
    var (coreslst, targetslst) = (cores.ToList(), targets.ToList());
    var (core_compare, core_desc) = (coreslst.Select(e => Json(e, false)), coreslst.Select(e => Json(e, true)));
    var (targets_compare, targets_desc) = (targetslst.Select(e => Json(e, false)), targetslst.Select(e => Json(e, true)));
    Assert.That(targets_compare, Is.EquivalentTo(core_compare), $"Checksum comparison failed\ncore entities:\n\t{String.Join("\n\t", core_desc)}\ntarget system entities[{targetsys}]:\n\t{String.Join("\n\t", targets_desc)}");
    
    string Json(object obj, bool includeid) => JsonSerializer.Serialize(obj, includeid ? withid : noid);
  }
}