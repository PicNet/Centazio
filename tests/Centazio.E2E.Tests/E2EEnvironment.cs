using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Centazio.Core;
using Centazio.Core.Checksum;
using Centazio.Core.CoreRepo;
using Centazio.Core.Ctl;
using Centazio.Core.Ctl.Entities;
using Centazio.Core.EntitySysMapping;
using Centazio.Core.Misc;
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
    Assert.That(actual.Count, Is.EqualTo(ascore.Count), $"ValidateAdded Type[{typeof(T).Name}] Expected[{ascore.Count}] Actual[{actual.Count}]" +
        $"\nExpected Items({ascore.Count}):\n\t" + String.Join("\n\t", ascore.Select(e => $"{e.DisplayName}({e.CoreId})")) + 
        $"\nActual Items({actual.Count}):\n\t" + String.Join("\n\t", actual.Select(e => $"{e.DisplayName}({e.CoreId})")));
  }

  public async Task ValidateUpdated<T>(params IEnumerable<ISystemEntity>[] expected) where T : ICoreEntity {
    var eadded = added.Values.Where(e => e.GetType() == typeof(T)).ToList();
    var ascore = (await SysEntsToCore(expected))
        .DistinctBy(e => (e.GetType(), Id: e.CoreId))
        // remove from validation if these entities were added in this epoch, as
        // they will be validated in the `ValidateAdded` method
        .Where(e => !eadded.Contains(e)) 
        .ToList();
    var actual = updated.Values.Where(e => e.GetType() == typeof(T)).ToList();
    Assert.That(actual.Count, Is.EqualTo(ascore.Count), $"ValidateUpdated Type[{typeof(T).Name}] Expected[{ascore.Count}] Actual[{actual.Count}]" +
        $"\nExpected Items({ascore.Count}):\n\t" + String.Join("\n\t", ascore.Select(e => $"{e.DisplayName}({e.CoreId})")) + 
        $"\nActual Items({actual.Count}):\n\t" + String.Join("\n\t", actual.Select(e => $"{e.DisplayName}({e.CoreId})")));
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
      var exid = ctx.entitymap.Db.SingleOrDefault(m => m.SystemId == e.SystemId)?.CoreId;
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
  public List<Map.CoreToSystem> Db => memdb.Values.ToList();
}

public class SimulationCtx {
  
  public readonly bool SILENCE_LOGGING = false;
  public readonly bool SILENCE_SIMULATION = false;
  public readonly bool ALLOW_BIDIRECTIONAL = true;
  public List<string> LOGGING_FILTERS { get; } = [];
  
  public readonly Random rng = new(1);
  // random but seedable guid
  public Guid Guid() { 
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
 
  public CoreCustomer CrmCustomerToCoreCustomer(CrmCustomer c, CoreCustomer? existing) => 
      existing is null 
          ? new(new(c.SystemId.Value), SimulationConstants.CRM_SYSTEM, c.Updated, c.Name, new(c.MembershipTypeSystemId.Value))
          : existing with { Name = c.Name, MembershipCoreId = new(c.MembershipTypeSystemId.Value) };

  public CoreInvoice CrmInvoiceToCoreInvoice(CrmInvoice i, CoreInvoice? existing, CoreEntityId? custcoreid = null) {
    custcoreid ??= entitymap.Db.Single(m => m.System == SimulationConstants.CRM_SYSTEM && m.CoreEntityType == CoreEntityType.From<CoreCustomer>() && m.SystemId == i.CustomerSystemId).CoreId;
    if (existing is not null && existing.CustomerCoreId != custcoreid) { throw new Exception("trying to change customer on an invoice which is not allowed"); }
    return existing is null 
        ? new CoreInvoice(new(i.SystemId.Value), SimulationConstants.CRM_SYSTEM, i.Updated, custcoreid, i.AmountCents, i.DueDate, i.PaidDate)
        : existing with { Cents = i.AmountCents, DueDate = i.DueDate, PaidDate = i.PaidDate };
  }
  
  public CoreCustomer FinAccountToCoreCustomer(FinAccount a, CoreCustomer? existing) => 
      existing is null 
          ? new CoreCustomer(new(a.SystemId.Value), SimulationConstants.FIN_SYSTEM, a.Updated, a.Name, new(CrmSystem.PENDING_MEMBERSHIP_TYPE_ID.ToString()))
          : existing with { Name = a.Name };

  public CoreInvoice FinInvoiceToCoreInvoice(FinInvoice i, CoreInvoice? existing, CoreEntityId? custcoreid = null) {
    custcoreid ??= entitymap.Db.Single(m => m.System == SimulationConstants.FIN_SYSTEM && m.CoreEntityType == CoreEntityType.From<CoreCustomer>() && m.SystemId == i.AccountSystemId).CoreId;
    if (existing is not null && existing.CustomerCoreId != custcoreid) { throw new Exception("trying to change customer on an invoice which is not allowed"); }
    return existing is null 
        ? new CoreInvoice(new(i.SystemId.Value), SimulationConstants.FIN_SYSTEM, i.Updated, custcoreid, (int)(i.Amount * 100), DateOnly.FromDateTime(i.DueDate), i.PaidDate) 
        : existing with { Cents = (int)(i.Amount * 100), DueDate = DateOnly.FromDateTime(i.DueDate), PaidDate = i.PaidDate };
  }

  public CoreMembershipType CrmMembershipTypeToCoreMembershipType(CrmMembershipType m, CoreMembershipType? existing) => existing is null 
      ? new(new(m.SystemId.Value), m.Updated, m.Name) 
      : existing with { Name = m.Name };
  
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
    var core_types = ctx.core.Types.Cast<CoreMembershipType>().Select(m => new { Id = m.CoreId, m.Name });
    var crm_types = crm.MembershipTypes.Select(m => new { Id = m.SystemId, m.Name } );
    
    await ctx.Epoch.ValidateAdded<CoreMembershipType>(ctx.Epoch.Epoch == 0 ? crm.MembershipTypes : []);
    await ctx.Epoch.ValidateUpdated<CoreMembershipType>(crm.Simulation.EditedMemberships);
    CompareByChecksum(SimulationConstants.CRM_SYSTEM, core_types, crm_types);
  }
  
  private async Task CompareCustomers() {
    var core_customers_for_crm = ctx.core.Customers.Cast<CoreCustomer>().Select(c => new { Id = c.CoreId, c.Name, MembershipTypeId = c.MembershipCoreId.Value });
    var core_customers_for_fin = ctx.core.Customers.Cast<CoreCustomer>().Select(c => new { Id = c.CoreId, c.Name });
    var crm_customers = crm.Customers.Select(c => new { Id = c.SystemId, c.Name, MembershipTypeId = c.MembershipTypeId.ToString() });
    var fin_accounts = fin.Accounts.Select(c => new { Id = c.SystemId, c.Name });
    
    await ctx.Epoch.ValidateAdded<CoreCustomer>(crm.Simulation.AddedCustomers, fin.Simulation.AddedAccounts);
    await ctx.Epoch.ValidateUpdated<CoreCustomer>(crm.Simulation.EditedCustomers, fin.Simulation.EditedAccounts);
    CompareByChecksum(SimulationConstants.CRM_SYSTEM, core_customers_for_crm, crm_customers);
    CompareByChecksum(SimulationConstants.FIN_SYSTEM, core_customers_for_fin, fin_accounts);
  }
  
  private async Task CompareInvoices() {
    var core_invoices = ctx.core.Invoices.Cast<CoreInvoice>().Select(i => new { Id = i.CoreId, i.PaidDate, i.DueDate, Amount = i.Cents }).ToList();
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
          if (ti.Kind != JsonTypeInfoKind.Object || ti.Type == typeof(CoreEntityId) || ti.Type == typeof(SystemEntityId)) return;
          var prop = ti.Properties.Single(p => p.Name == "Id");
          ti.Properties.Remove(prop);
        }
      }
    }
  };
  
  private void CompareByChecksum(SystemName system, IEnumerable<object> cores, IEnumerable<object> targets) {
    var (coreslst, targetslst) = (cores.ToList(), targets.ToList());
    var (core_compare, targets_compare) = (coreslst.Select(e => Json(e, false)), targetslst.Select(e => Json(e, false)));
    var (core_desc, targets_desc) = (coreslst.Select(e => Json(e, true)), targetslst.Select(e => Json(e, true)));
    Assert.That(targets_compare, Is.EquivalentTo(core_compare), $"[{system}] checksum comparison failed\ncore entities:\n\t{String.Join("\n\t", core_desc)}\ntarget system entities:\n\t{String.Join("\n\t", targets_desc)}");
    
    string Json(object obj, bool includeid) => JsonSerializer.Serialize(obj, includeid ? withid : noid);
  }
}